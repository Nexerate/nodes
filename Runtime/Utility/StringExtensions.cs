#region Using
using System.Text; 
#endregion

namespace Nexerate.Nodes
{
    public static class StringExtensions
    {
        #region Space Before Uppercase
        //Simplified solution of: https://stackoverflow.com/questions/272633/add-spaces-before-capital-letters
        /// <summary>
        /// Add a space in front of all uppercase letters in the string.
        /// </summary>
        public static string SpaceBeforeUppercase(this string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return string.Empty;
            StringBuilder newText = new(target.Length * 2);
            newText.Append(target[0]);
            for (int i = 1; i < target.Length; i++)
            {
                if (char.IsUpper(target[i]) && ((target[i - 1] != ' ' && !char.IsUpper(target[i - 1])) || (char.IsUpper(target[i - 1]) && i < target.Length - 1 && !char.IsUpper(target[i + 1]))))
                    newText.Append(' ');
                newText.Append(target[i]);
            }
            return newText.ToString();
        }
        #endregion

        #region Uppercase
        /// <summary>
        /// Make the first char in a string uppercase.
        /// </summary>
        public static string FirstUppercase(this string target) => char.ToUpper(target[0]) + target[1..];
        #endregion

        #region Lowercase
        /// <summary>
        /// Make the first char in a string lowercase.
        /// </summary>
        public static string FirstLowercase(this string target) => char.ToLower(target[0]) + target[1..];
        #endregion

        #region Remove
        /// <summary>
        /// Remove all instances of <paramref name="remove"/> from the string.
        /// </summary>
        public static string Remove(this string target, string remove) => target.Replace(remove, "");
        #endregion

        #region Remove Spaces
        /// <summary>
        /// Remove all spaces from the string.
        /// </summary>
        public static string RemoveSpaces(this string target) => target.Remove(" ");
        #endregion
    }
}
