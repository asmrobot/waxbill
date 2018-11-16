namespace ZTImage.Net.Utils
{
    using System;

    public static class Preconditions
    {
        public static T CheckNotNull<T>(T obj, string msg) where T: class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(msg);
            }
            return obj;
        }
    }
}

