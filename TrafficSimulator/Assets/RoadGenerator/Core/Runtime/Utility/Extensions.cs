using System.Collections.Generic;

namespace Extensions
{
    public static class Util
    {
        public static void StackOnTop<T>(this Stack<T> stack1, Stack<T> stack2)
        {
            T[] arr = new T[stack2.Count];
            stack2.CopyTo(arr, 0);

            for (int i = arr.Length - 1; i >= 0; i--)
                stack1.Push(arr[i]);
        }

        public static void StackBelow<T>(this Stack<T> stack1, Stack<T> stack2)
        {
            // Move the original contents of stack1 to a temporary stack
            Stack<T> temp1 = new Stack<T>();
            while(stack1.Count > 0)
                temp1.Push(stack1.Pop());
            
            // Copy the contents of stack2 to a temporary stack
            T[] arr = new T[stack2.Count];
            stack2.CopyTo(arr, 0);
            Stack<T> temp2 = new Stack<T>(arr);

            // Add back the contents of stack2 first
            while(temp2.Count > 0)
                stack1.Push(temp2.Pop());

            // Then, add back the original contents of stack1
            while(temp1.Count > 0)
                stack1.Push(temp1.Pop());
        }
    }
}