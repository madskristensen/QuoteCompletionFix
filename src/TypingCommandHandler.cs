using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace QuoteCompletionFix
{
    [Export(typeof(ICommandHandler))]
    [Name(nameof(TypingCommandHandler))]
    [ContentType(ContentTypes.CSharp)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal class TypingCommandHandler : ICommandHandler<TypeCharCommandArgs>
    {
        public string DisplayName => throw new NotImplementedException();

        public bool ExecuteCommand(TypeCharCommandArgs args, CommandExecutionContext executionContext)
        {
            if (args.TypedChar is not '\'' and not '"')
            {
                return false;
            }

            ITextSnapshot snapshot = args.TextView.TextSnapshot;
            var position = args.TextView.Caret.Position.BufferPosition.Position;
            var next = position < snapshot.Length ? snapshot.GetText(position, 1)[0] : ' ';
            var prev = position > 0 ? snapshot.GetText(position - 1, 1)[0] : ' ';

            // Preserve current behavior when the caret is in between two quotation marks
            if (prev == args.TypedChar && next == args.TypedChar)
            {
                return false;
            }

            // If the next char is the typed char, then revert to existing behavior
            // to support type-through (provisional text)
            if (next == args.TypedChar)
            {
                return false;
            }

            // If the next char is a letter or digit,
            // insert the typed char and stop further processing resulting in auto-completion
            if (char.IsLetterOrDigit(next))
            {
                args.TextView.TextBuffer.Insert(position, args.TypedChar.ToString());
                return true;
            }

            // If the next char is a letter or digit or a $ or @,
            // insert the typed char and stop further processing resulting in auto-completion
            if (char.IsLetterOrDigit(prev) || prev is '$' or '@')
            {
                args.TextView.TextBuffer.Insert(position, "" + args.TypedChar + args.TypedChar);
                args.TextView.Caret.MoveToPreviousCaretPosition();
                return true;
            }

            return false;
        }

        public CommandState GetCommandState(TypeCharCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}
