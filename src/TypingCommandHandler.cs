using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Commanding;
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

            var position = args.TextView.Caret.Position.BufferPosition.Position;

            // Look at the next character
            if (position < args.TextView.TextSnapshot.Length)
            {
                var next = args.TextView.TextSnapshot.GetText(position, 1)[0];

                // If the next char is a letter or digit, insert the typed char and stop further processing resulting in auto-completion
                if (char.IsLetterOrDigit(next))
                {
                    args.TextView.TextBuffer.Insert(position, args.TypedChar.ToString());
                    return true;
                }
            }

            // Then look at the previous character
            if (position > 0)
            {
                var prev = args.TextView.TextSnapshot.GetText(position - 1, 1)[0];

                // If the next char is a letter or digit, but not a $ or @,
                // insert the typed char and stop further processing resulting in auto-completion
                if (char.IsLetterOrDigit(prev) && prev is not '$' and not '@')
                {
                    args.TextView.TextBuffer.Insert(position, args.TypedChar.ToString());
                    return true;
                }
            }

            return false;
        }

        public CommandState GetCommandState(TypeCharCommandArgs args)
        {
            return CommandState.Available;
        }
    }
}
