using System;
using System.Collections.Generic;

namespace Sitecore.Support.Form.Core.Visual
{
    internal class CategoryComparer : IComparer<VisualPropertyInfo>
    {
        public int Compare(VisualPropertyInfo x, VisualPropertyInfo y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return 0;
                }
                return -1;
            }
            else
            {
                if (y == null)
                {
                    return 1;
                }
                if (x.CategorySortOrder != -1 && y.CategorySortOrder != -1)
                {
                    return x.CategorySortOrder.CompareTo(y.CategorySortOrder);
                }
                if (x.CategorySortOrder != -1)
                {
                    return x.CategorySortOrder.CompareTo(x.CategorySortOrder + 1);
                }
                if (y.CategorySortOrder != -1)
                {
                    return (y.CategorySortOrder + 1).CompareTo(y.CategorySortOrder);
                }
                return string.Compare(x.Category, y.Category, StringComparison.Ordinal);
            }
        }
    }
}
