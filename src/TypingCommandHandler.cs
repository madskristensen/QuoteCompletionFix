using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace QuoteCompletionFix
{
    [Export(typeof(ICommandHandler))]
    [Name(nameof(TypingCommandHandler))]
    [ContentType(ContentTypes.CSharp)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal class TypingCommandHandler : ICommandHandler<TypeCharCommandArgs>
    {
        public string DisplayName => nameof(TypingCommandHandler);
        private static RatingPrompt _rating;

        [Import]
        public IEditorOperationsFactoryService EditorOperationsFactory { get; set; }

        // Returning true means that further processing of the typed char should be stopped (we handled it).
        // Returning false means that the built-in behavior should be used after this handler (we didn't handle it).
        public bool ExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            // Pass through if typed char isn't a quote
            if (args.TypedChar is not '"')
            {
                return false;
            }

            ITextSnapshot snapshot = args.TextView.TextSnapshot;
            var position = args.TextView.Caret.Position.BufferPosition.Position;
            var next = position < snapshot.Length ? snapshot.GetText(position, 1)[0] : ' ';
            var prev = position > 0 ? snapshot.GetText(position - 1, 1)[0] : ' ';

            // Pass through to preserve current behavior when the caret is in between two quotation marks
            if (prev == args.TypedChar && next == args.TypedChar)
            {
                return false;
            }

            // Pass through if the next char is a quote to support type-through (provisional text)
            // TODO: Check for existence of provisional text and only return false if it exists
            if (next == args.TypedChar)
            {
                return false;
            }

            // We're in a state where we can run our rules
            return ApplyRules(args, position, next, prev);
        }

        private bool ApplyRules(TypeCharCommandArgs args, int position, char next, char prev)
        {
            // Rule #1: If the next char is a letter or digit, don't auto-complete the quote
            if (char.IsLetterOrDigit(next) || char.IsLetterOrDigit(prev))
            {
                args.TextView.TextBuffer.Insert(position, args.TypedChar.ToString());
                RegisterUseAsync().FireAndForget();
                return true;
            }

            // Rule #2: If the previous char is a $ or @, auto-complete the quote
            if (prev is '$' or '@')
            {
                args.TextView.TextBuffer.Insert(position, "" + args.TypedChar);
                EditorOperationsFactory.GetEditorOperations(args.TextView).InsertProvisionalText("\"");
                args.TextView.Caret.MoveToPreviousCaretPosition();
                RegisterUseAsync().FireAndForget();
                return true;
            }

            return false; // No rules applied from this command handler. Pass through to built-in command handlers
        }

        private static async Task RegisterUseAsync()
        {
            _rating ??= new("MadsKristensen.QuoteCompletionFix", Vsix.Name, await General.GetLiveInstanceAsync());
            _rating.RegisterSuccessfulUsage();
        }

        public CommandState GetCommandState(TypeCharCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}
