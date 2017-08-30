using System.Collections.Generic;

namespace UUebView {
    public enum HTMLAttribute {
		_UNKNOWN,

        // system.
        _CONTENT,
		_BOX,
        _COLLISION,
        _ONLAYOUT_PRESET_X,
        _LAYER_PARENT_TYPE,

        // attributes.
        ID,
        LISTEN,
        BUTTON,
        HIDDEN,
        SRC,
        HREF,
    }

    public class AttributeKVs : Dictionary<HTMLAttribute, object> {}
}