namespace UUebView
{
    public class ConstSettings
    {
        public const string UUEBVIEW_DECL = "<!DOCTYPE uuebview href=";
        public const int TAG_MAX_LEN = 100;
        public const double TIMEOUT_SEC = 10.0;

        public static readonly object[] ShouldInheritAttributes = new object[]{
            HTMLAttribute.HREF,
        };

        public const string listFileName = "UUebTags.txt";

        public const string ROOTVIEW_NAME = "UUebViewRoot";

        public const string UUEBTAGS_DEFAULT = "Default";
        public const string FULLPATH_INFORMATION_RESOURCE = "Assets/UUebView/GeneratedResources/Resources/Views/";

        public const string FULLPATH_DEFAULT_TAGS = FULLPATH_INFORMATION_RESOURCE + UUEBTAGS_DEFAULT + "/";
        public const string PREFIX_PATH_INFORMATION_RESOURCE = "Views/";


        public const string CONNECTIONID_DOWNLOAD_HTML_PREFIX = "download_html_";
        public const string CONNECTIONID_DOWNLOAD_UUEBTAGS_PREFIX = "download_uuebTags_";
        public const string CONNECTIONID_DOWNLOAD_IMAGE_PREFIX = "download_image_";
    }
}
