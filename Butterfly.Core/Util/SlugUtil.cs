using System.Text.RegularExpressions;

namespace Butterfly.Core.Util {
    public static class SlugUtil {

        readonly static Regex NON_SLUG = new Regex(@"[^a-z0-9\-]");

        public static string Slugify(string name, bool throwExceptionIfEmpty = true) {
            var slug = name.ToLower().Replace(" ", "-");
            slug = NON_SLUG.Replace(slug, "").Replace("--", "-");
            if (throwExceptionIfEmpty && string.IsNullOrEmpty(slug)) throw new System.Exception("Slug cannot be empty");
            return slug;
        }
    }
}
