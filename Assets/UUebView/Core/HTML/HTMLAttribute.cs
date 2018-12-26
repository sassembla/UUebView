using System.Collections.Generic;

namespace UUebView
{
    public enum HTMLAttribute
    {
        _UNKNOWN,

        // system.
        _CONTENT,
        _IS_SINGLE_LINE,

        _BOX,
        _COLLISION,
        _ONLAYOUT_PRESET_X,
        _LAYER_PARENT_TYPE,
        _ONLAYOUT_LAYOUTED_OFFSET_Y,

        // attributes.

        // html attr.
        ID,
        SRC,
        HREF,
        ALIGN,

        // special attr.
        LISTEN,
        BUTTON,
        HIDDEN,
    }

    public class AttributeKVs : Dictionary<HTMLAttribute, object> { }
}
