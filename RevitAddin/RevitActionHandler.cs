using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraRevit.RevitAddin
{
    public sealed class RevitActionHandler : IExternalEventHandler
    {
        private readonly object _sync = new object();
        private RevitAction _pendingAction;
        private TaskCompletionSource<RevitActionResult> _pendingCompletion;

        public RevitActionHandler()
        {
            ExternalEvent = ExternalEvent.Create(this);
        }

        public ExternalEvent ExternalEvent { get; private set; }

        public Task<RevitActionResult> RaiseAsync(RevitAction action)
        {
            if (action == null)
            {
                return Task.FromResult(RevitActionResult.Failure("No Revit action was supplied."));
            }

            var completion = new TaskCompletionSource<RevitActionResult>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_sync)
            {
                if (_pendingAction != null)
                {
                    completion.SetResult(RevitActionResult.Failure("Another Revit action is already running."));
                    return completion.Task;
                }

                _pendingAction = action;
                _pendingCompletion = completion;
            }

            try
            {
                ExternalEvent.Raise();
            }
            catch (Exception exception)
            {
                Complete(RevitActionResult.Failure("Unable to schedule the Revit action: " + exception.Message));
            }

            return completion.Task;
        }

        public void Execute(UIApplication application)
        {
            RevitAction action;
            lock (_sync)
            {
                action = _pendingAction;
            }

            if (action == null)
            {
                return;
            }

            try
            {
                if (action.IsSelectAction)
                {
                    Complete(SelectWalls(application, action.Query));
                }
                else
                {
                    Complete(RevitActionResult.Failure("The Revit action type is not supported by this handler."));
                }
            }
            catch (Exception exception)
            {
                Complete(RevitActionResult.Failure("Revit rejected the action: " + exception.Message));
            }
        }

        public string GetName()
        {
            return "Aurora AI Revit Action Handler";
        }

        private RevitActionResult SelectWalls(UIApplication application, string query)
        {
            if (application == null || application.ActiveUIDocument == null)
            {
                return RevitActionResult.Failure("Open a Revit document before asking Aurora to select elements.");
            }

            if (string.IsNullOrWhiteSpace(query) || query.IndexOf("wall", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return RevitActionResult.Failure("The select action currently supports wall queries, for example: All walls.");
            }

            var uidoc = application.ActiveUIDocument;
            var doc = uidoc.Document;
            IList<ElementId> wallIds;

            // Keep the complete Revit-side action inside the explicit transaction
            // boundary requested for AI actions, including the selection assignment.
            using (Transaction tx = new Transaction(doc, "AI Action"))
            {
                tx.Start();
                wallIds = new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType()
                    .ToElementIds()
                    .ToList();
                uidoc.Selection.SetElementIds(wallIds);
                tx.Commit();
            }
            return RevitActionResult.Success(
                wallIds.Count == 0
                    ? "No wall instances were found in the active document."
                    : $"Selected {wallIds.Count} wall instance(s) in the active document.");
        }

        private void Complete(RevitActionResult result)
        {
            TaskCompletionSource<RevitActionResult> completion;
            lock (_sync)
            {
                completion = _pendingCompletion;
                _pendingAction = null;
                _pendingCompletion = null;
            }

            if (completion != null)
            {
                completion.TrySetResult(result);
            }
        }
    }

    public sealed class RevitActionResult
    {
        private RevitActionResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; private set; }
        public string Message { get; private set; }

        public static RevitActionResult Success(string message)
        {
            return new RevitActionResult(true, message);
        }

        public static RevitActionResult Failure(string message)
        {
            return new RevitActionResult(false, message);
        }
    }
}
