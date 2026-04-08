using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Text;

namespace FakeXrmEasy.Extensions
{
    /// <summary>
    /// Extension methods for EntityReference
    /// </summary>
    public static class EntityReferenceExtensions
    {
        /// <summary>
        /// Determines whether the entity reference has key attributes
        /// </summary>
        /// <param name="er">The entity reference</param>
        /// <returns>True if the entity reference has key attributes, false otherwise</returns>
        public static bool HasKeyAttributes(this EntityReference er)
        {
            if(er == null)
            {
                return false;
            }

#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
            return er.KeyAttributes.Count > 0;
#else
            return false;
#endif
        }
    }
}
