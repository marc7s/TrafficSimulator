using UnityEngine.UIElements;

namespace UI
{
    public class NonFocusableButton : Button
    {
        public NonFocusableButton()
        {
            focusable = false;
        }
    }
}