using MischievousByte.Masquerade;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MischievousByte.Silhouette
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class PoserAttribute : Attribute
    {
        public readonly BodyNode Target;

        public PoserAttribute(BodyNode target) => Target = target;
    }
}
