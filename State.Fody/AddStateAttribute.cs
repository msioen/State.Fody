using System;

namespace State.Fody
{
    /// <summary>
    /// Hooks the state plugin up for the attributed method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AddStateAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:State.Fody.AddStateAttribute"/> class.
        /// </summary>
        /// <param name="propertyName">The boolean property whose state will be set by the State plugin</param>
        public AddStateAttribute(string propertyName)
        {
        }
    }
}
