public partial class WebGL
{
    /* ClearBufferMask */
    public const int DEPTH_BUFFER_BIT               = 0x00000100;
    public const int STENCIL_BUFFER_BIT             = 0x00000400;
    public const int COLOR_BUFFER_BIT               = 0x00004000;

    /* BeginMode */
    public const int POINTS                         = 0x0000;
    public const int LINES                          = 0x0001;
    public const int LINE_LOOP                      = 0x0002;
    public const int LINE_STRIP                     = 0x0003;
    public const int TRIANGLES                      = 0x0004;
    public const int TRIANGLE_STRIP                 = 0x0005;
    public const int TRIANGLE_FAN                   = 0x0006;

    /* AlphaFunction (not supported in ES20) */
    /*      NEVER */
    /*      LESS */
    /*      EQUAL */
    /*      LEQUAL */
    /*      GREATER */
    /*      NOTEQUAL */
    /*      GEQUAL */
    /*      ALWAYS */

    /* BlendingFactorDest */
    public const int ZERO                           = 0;
    public const int ONE                            = 1;
    public const int SRC_COLOR                      = 0x0300;
    public const int ONE_MINUS_SRC_COLOR            = 0x0301;
    public const int SRC_ALPHA                      = 0x0302;
    public const int ONE_MINUS_SRC_ALPHA            = 0x0303;
    public const int DST_ALPHA                      = 0x0304;
    public const int ONE_MINUS_DST_ALPHA            = 0x0305;

    /* BlendingFactorSrc */
    /*      ZERO */
    /*      ONE */
    public const int DST_COLOR                      = 0x0306;
    public const int ONE_MINUS_DST_COLOR            = 0x0307;
    public const int SRC_ALPHA_SATURATE             = 0x0308;
    /*      SRC_ALPHA */
    /*      ONE_MINUS_SRC_ALPHA */
    /*      DST_ALPHA */
    /*      ONE_MINUS_DST_ALPHA */

    /* BlendEquationSeparate */
    public const int FUNC_ADD                       = 0x8006;
    public const int BLEND_EQUATION                 = 0x8009;
    public const int BLEND_EQUATION_RGB             = 0x8009;   /* same as BLEND_EQUATION */
    public const int BLEND_EQUATION_ALPHA           = 0x883D;

    /* BlendSubtract */
    public const int FUNC_SUBTRACT                  = 0x800A;
    public const int FUNC_REVERSE_SUBTRACT          = 0x800B;

    /* Separate Blend Functions */
    public const int BLEND_DST_RGB                  = 0x80C8;
    public const int BLEND_SRC_RGB                  = 0x80C9;
    public const int BLEND_DST_ALPHA                = 0x80CA;
    public const int BLEND_SRC_ALPHA                = 0x80CB;
    public const int CONSTANT_COLOR                 = 0x8001;
    public const int ONE_MINUS_CONSTANT_COLOR       = 0x8002;
    public const int CONSTANT_ALPHA                 = 0x8003;
    public const int ONE_MINUS_CONSTANT_ALPHA       = 0x8004;
    public const int BLEND_COLOR                    = 0x8005;

    /* Buffer Objects */
    public const int ARRAY_BUFFER                   = 0x8892;
    public const int ELEMENT_ARRAY_BUFFER           = 0x8893;
    public const int ARRAY_BUFFER_BINDING           = 0x8894;
    public const int ELEMENT_ARRAY_BUFFER_BINDING   = 0x8895;

    public const int STREAM_DRAW                    = 0x88E0;
    public const int STATIC_DRAW                    = 0x88E4;
    public const int DYNAMIC_DRAW                   = 0x88E8;

    public const int BUFFER_SIZE                    = 0x8764;
    public const int BUFFER_USAGE                   = 0x8765;

    public const int CURRENT_VERTEX_ATTRIB          = 0x8626;

    /* CullFaceMode */
    public const int FRONT                          = 0x0404;
    public const int BACK                           = 0x0405;
    public const int FRONT_AND_BACK                 = 0x0408;

    /* DepthFunction */
    /*      NEVER */
    /*      LESS */
    /*      EQUAL */
    /*      LEQUAL */
    /*      GREATER */
    /*      NOTEQUAL */
    /*      GEQUAL */
    /*      ALWAYS */

    /* EnableCap */
    /* TEXTURE_2D */
    public const int CULL_FACE                      = 0x0B44;
    public const int BLEND                          = 0x0BE2;
    public const int DITHER                         = 0x0BD0;
    public const int STENCIL_TEST                   = 0x0B90;
    public const int DEPTH_TEST                     = 0x0B71;
    public const int SCISSOR_TEST                   = 0x0C11;
    public const int POLYGON_OFFSET_FILL            = 0x8037;
    public const int SAMPLE_ALPHA_TO_COVERAGE       = 0x809E;
    public const int SAMPLE_COVERAGE                = 0x80A0;

    /* ErrorCode */
    public const int NO_ERROR                       = 0;
    public const int INVALID_ENUM                   = 0x0500;
    public const int INVALID_VALUE                  = 0x0501;
    public const int INVALID_OPERATION              = 0x0502;
    public const int OUT_OF_MEMORY                  = 0x0505;

    /* FrontFaceDirection */
    public const int CW                             = 0x0900;
    public const int CCW                            = 0x0901;

    /* GetPName */
    public const int LINE_WIDTH                     = 0x0B21;
    public const int ALIASED_POINT_SIZE_RANGE       = 0x846D;
    public const int ALIASED_LINE_WIDTH_RANGE       = 0x846E;
    public const int CULL_FACE_MODE                 = 0x0B45;
    public const int FRONT_FACE                     = 0x0B46;
    public const int DEPTH_RANGE                    = 0x0B70;
    public const int DEPTH_WRITEMASK                = 0x0B72;
    public const int DEPTH_CLEAR_VALUE              = 0x0B73;
    public const int DEPTH_FUNC                     = 0x0B74;
    public const int STENCIL_CLEAR_VALUE            = 0x0B91;
    public const int STENCIL_FUNC                   = 0x0B92;
    public const int STENCIL_FAIL                   = 0x0B94;
    public const int STENCIL_PASS_DEPTH_FAIL        = 0x0B95;
    public const int STENCIL_PASS_DEPTH_PASS        = 0x0B96;
    public const int STENCIL_REF                    = 0x0B97;
    public const int STENCIL_VALUE_MASK             = 0x0B93;
    public const int STENCIL_WRITEMASK              = 0x0B98;
    public const int STENCIL_BACK_FUNC              = 0x8800;
    public const int STENCIL_BACK_FAIL              = 0x8801;
    public const int STENCIL_BACK_PASS_DEPTH_FAIL   = 0x8802;
    public const int STENCIL_BACK_PASS_DEPTH_PASS   = 0x8803;
    public const int STENCIL_BACK_REF               = 0x8CA3;
    public const int STENCIL_BACK_VALUE_MASK        = 0x8CA4;
    public const int STENCIL_BACK_WRITEMASK         = 0x8CA5;
    public const int VIEWPORT                       = 0x0BA2;
    public const int SCISSOR_BOX                    = 0x0C10;
    /*      SCISSOR_TEST */
    public const int COLOR_CLEAR_VALUE              = 0x0C22;
    public const int COLOR_WRITEMASK                = 0x0C23;
    public const int UNPACK_ALIGNMENT               = 0x0CF5;
    public const int PACK_ALIGNMENT                 = 0x0D05;
    public const int MAX_TEXTURE_SIZE               = 0x0D33;
    public const int MAX_VIEWPORT_DIMS              = 0x0D3A;
    public const int SUBPIXEL_BITS                  = 0x0D50;
    public const int RED_BITS                       = 0x0D52;
    public const int GREEN_BITS                     = 0x0D53;
    public const int BLUE_BITS                      = 0x0D54;
    public const int ALPHA_BITS                     = 0x0D55;
    public const int DEPTH_BITS                     = 0x0D56;
    public const int STENCIL_BITS                   = 0x0D57;
    public const int POLYGON_OFFSET_UNITS           = 0x2A00;
    /*      POLYGON_OFFSET_FILL */
    public const int POLYGON_OFFSET_FACTOR          = 0x8038;
    public const int TEXTURE_BINDING_2D             = 0x8069;
    public const int SAMPLE_BUFFERS                 = 0x80A8;
    public const int SAMPLES                        = 0x80A9;
    public const int SAMPLE_COVERAGE_VALUE          = 0x80AA;
    public const int SAMPLE_COVERAGE_INVERT         = 0x80AB;

    /* GetTextureParameter */
    /*      TEXTURE_MAG_FILTER */
    /*      TEXTURE_MIN_FILTER */
    /*      TEXTURE_WRAP_S */
    /*      TEXTURE_WRAP_T */

    public const int COMPRESSED_TEXTURE_FORMATS     = 0x86A3;

    /* HintMode */
    public const int DONT_CARE                      = 0x1100;
    public const int FASTEST                        = 0x1101;
    public const int NICEST                         = 0x1102;

    /* HintTarget */
    public const int GENERATE_MIPMAP_HINT            = 0x8192;

    /* DataType */
    public const int BYTE                           = 0x1400;
    public const int UNSIGNED_BYTE                  = 0x1401;
    public const int SHORT                          = 0x1402;
    public const int UNSIGNED_SHORT                 = 0x1403;
    public const int INT                            = 0x1404;
    public const int UNSIGNED_INT                   = 0x1405;
    public const int FLOAT                          = 0x1406;

    /* PixelFormat */
    public const int DEPTH_COMPONENT                = 0x1902;
    public const int ALPHA                          = 0x1906;
    public const int RGB                            = 0x1907;
    public const int RGBA                           = 0x1908;
    public const int LUMINANCE                      = 0x1909;
    public const int LUMINANCE_ALPHA                = 0x190A;

    /* PixelType */
    /*      UNSIGNED_BYTE */
    public const int UNSIGNED_SHORT_4_4_4_4         = 0x8033;
    public const int UNSIGNED_SHORT_5_5_5_1         = 0x8034;
    public const int UNSIGNED_SHORT_5_6_5           = 0x8363;

    /* Shaders */
    public const int FRAGMENT_SHADER                  = 0x8B30;
    public const int VERTEX_SHADER                    = 0x8B31;
    public const int MAX_VERTEX_ATTRIBS               = 0x8869;
    public const int MAX_VERTEX_UNIFORM_VECTORS       = 0x8DFB;
    public const int MAX_VARYING_VECTORS              = 0x8DFC;
    public const int MAX_COMBINED_TEXTURE_IMAGE_UNITS = 0x8B4D;
    public const int MAX_VERTEX_TEXTURE_IMAGE_UNITS   = 0x8B4C;
    public const int MAX_TEXTURE_IMAGE_UNITS          = 0x8872;
    public const int MAX_FRAGMENT_UNIFORM_VECTORS     = 0x8DFD;
    public const int SHADER_TYPE                      = 0x8B4F;
    public const int DELETE_STATUS                    = 0x8B80;
    public const int LINK_STATUS                      = 0x8B82;
    public const int VALIDATE_STATUS                  = 0x8B83;
    public const int ATTACHED_SHADERS                 = 0x8B85;
    public const int ACTIVE_UNIFORMS                  = 0x8B86;
    public const int ACTIVE_ATTRIBUTES                = 0x8B89;
    public const int SHADING_LANGUAGE_VERSION         = 0x8B8C;
    public const int CURRENT_PROGRAM                  = 0x8B8D;

    /* StencilFunction */
    public const int NEVER                          = 0x0200;
    public const int LESS                           = 0x0201;
    public const int EQUAL                          = 0x0202;
    public const int LEQUAL                         = 0x0203;
    public const int GREATER                        = 0x0204;
    public const int NOTEQUAL                       = 0x0205;
    public const int GEQUAL                         = 0x0206;
    public const int ALWAYS                         = 0x0207;

    /* StencilOp */
    /*      ZERO */
    public const int KEEP                           = 0x1E00;
    public const int REPLACE                        = 0x1E01;
    public const int INCR                           = 0x1E02;
    public const int DECR                           = 0x1E03;
    public const int INVERT                         = 0x150A;
    public const int INCR_WRAP                      = 0x8507;
    public const int DECR_WRAP                      = 0x8508;

    /* StringName */
    public const int VENDOR                         = 0x1F00;
    public const int RENDERER                       = 0x1F01;
    public const int VERSION                        = 0x1F02;

    /* TextureMagFilter */
    public const int NEAREST                        = 0x2600;
    public const int LINEAR                         = 0x2601;

    /* TextureMinFilter */
    /*      NEAREST */
    /*      LINEAR */
    public const int NEAREST_MIPMAP_NEAREST         = 0x2700;
    public const int LINEAR_MIPMAP_NEAREST          = 0x2701;
    public const int NEAREST_MIPMAP_LINEAR          = 0x2702;
    public const int LINEAR_MIPMAP_LINEAR           = 0x2703;

    /* TextureParameterName */
    public const int TEXTURE_MAG_FILTER             = 0x2800;
    public const int TEXTURE_MIN_FILTER             = 0x2801;
    public const int TEXTURE_WRAP_S                 = 0x2802;
    public const int TEXTURE_WRAP_T                 = 0x2803;

    /* TextureTarget */
    public const int TEXTURE_2D                     = 0x0DE1;
    public const int TEXTURE                        = 0x1702;

    public const int TEXTURE_CUBE_MAP               = 0x8513;
    public const int TEXTURE_BINDING_CUBE_MAP       = 0x8514;
    public const int TEXTURE_CUBE_MAP_POSITIVE_X    = 0x8515;
    public const int TEXTURE_CUBE_MAP_NEGATIVE_X    = 0x8516;
    public const int TEXTURE_CUBE_MAP_POSITIVE_Y    = 0x8517;
    public const int TEXTURE_CUBE_MAP_NEGATIVE_Y    = 0x8518;
    public const int TEXTURE_CUBE_MAP_POSITIVE_Z    = 0x8519;
    public const int TEXTURE_CUBE_MAP_NEGATIVE_Z    = 0x851A;
    public const int MAX_CUBE_MAP_TEXTURE_SIZE      = 0x851C;

    /* TextureUnit */
    public const int TEXTURE0                       = 0x84C0;
    public const int TEXTURE1                       = 0x84C1;
    public const int TEXTURE2                       = 0x84C2;
    public const int TEXTURE3                       = 0x84C3;
    public const int TEXTURE4                       = 0x84C4;
    public const int TEXTURE5                       = 0x84C5;
    public const int TEXTURE6                       = 0x84C6;
    public const int TEXTURE7                       = 0x84C7;
    public const int TEXTURE8                       = 0x84C8;
    public const int TEXTURE9                       = 0x84C9;
    public const int TEXTURE10                      = 0x84CA;
    public const int TEXTURE11                      = 0x84CB;
    public const int TEXTURE12                      = 0x84CC;
    public const int TEXTURE13                      = 0x84CD;
    public const int TEXTURE14                      = 0x84CE;
    public const int TEXTURE15                      = 0x84CF;
    public const int TEXTURE16                      = 0x84D0;
    public const int TEXTURE17                      = 0x84D1;
    public const int TEXTURE18                      = 0x84D2;
    public const int TEXTURE19                      = 0x84D3;
    public const int TEXTURE20                      = 0x84D4;
    public const int TEXTURE21                      = 0x84D5;
    public const int TEXTURE22                      = 0x84D6;
    public const int TEXTURE23                      = 0x84D7;
    public const int TEXTURE24                      = 0x84D8;
    public const int TEXTURE25                      = 0x84D9;
    public const int TEXTURE26                      = 0x84DA;
    public const int TEXTURE27                      = 0x84DB;
    public const int TEXTURE28                      = 0x84DC;
    public const int TEXTURE29                      = 0x84DD;
    public const int TEXTURE30                      = 0x84DE;
    public const int TEXTURE31                      = 0x84DF;
    public const int ACTIVE_TEXTURE                 = 0x84E0;

    /* TextureWrapMode */
    public const int REPEAT                         = 0x2901;
    public const int CLAMP_TO_EDGE                  = 0x812F;
    public const int MIRRORED_REPEAT                = 0x8370;

    /* Uniform Types */
    public const int FLOAT_VEC2                     = 0x8B50;
    public const int FLOAT_VEC3                     = 0x8B51;
    public const int FLOAT_VEC4                     = 0x8B52;
    public const int INT_VEC2                       = 0x8B53;
    public const int INT_VEC3                       = 0x8B54;
    public const int INT_VEC4                       = 0x8B55;
    public const int BOOL                           = 0x8B56;
    public const int BOOL_VEC2                      = 0x8B57;
    public const int BOOL_VEC3                      = 0x8B58;
    public const int BOOL_VEC4                      = 0x8B59;
    public const int FLOAT_MAT2                     = 0x8B5A;
    public const int FLOAT_MAT3                     = 0x8B5B;
    public const int FLOAT_MAT4                     = 0x8B5C;
    public const int SAMPLER_2D                     = 0x8B5E;
    public const int SAMPLER_CUBE                   = 0x8B60;

    /* Vertex Arrays */
    public const int VERTEX_ATTRIB_ARRAY_ENABLED        = 0x8622;
    public const int VERTEX_ATTRIB_ARRAY_SIZE           = 0x8623;
    public const int VERTEX_ATTRIB_ARRAY_STRIDE         = 0x8624;
    public const int VERTEX_ATTRIB_ARRAY_TYPE           = 0x8625;
    public const int VERTEX_ATTRIB_ARRAY_NORMALIZED     = 0x886A;
    public const int VERTEX_ATTRIB_ARRAY_POINTER        = 0x8645;
    public const int VERTEX_ATTRIB_ARRAY_BUFFER_BINDING = 0x889F;

    /* Read Format */
    public const int IMPLEMENTATION_COLOR_READ_TYPE   = 0x8B9A;
    public const int IMPLEMENTATION_COLOR_READ_FORMAT = 0x8B9B;

    /* Shader Source */
    public const int COMPILE_STATUS                 = 0x8B81;

    /* Shader Precision-Specified Types */
    public const int LOW_FLOAT                      = 0x8DF0;
    public const int MEDIUM_FLOAT                   = 0x8DF1;
    public const int HIGH_FLOAT                     = 0x8DF2;
    public const int LOW_INT                        = 0x8DF3;
    public const int MEDIUM_INT                     = 0x8DF4;
    public const int HIGH_INT                       = 0x8DF5;

    /* Framebuffer Object. */
    public const int FRAMEBUFFER                    = 0x8D40;
    public const int RENDERBUFFER                   = 0x8D41;

    public const int RGBA4                          = 0x8056;
    public const int RGB5_A1                        = 0x8057;
    public const int RGB565                         = 0x8D62;
    public const int DEPTH_COMPONENT16              = 0x81A5;
    public const int STENCIL_INDEX8                 = 0x8D48;
    public const int DEPTH_STENCIL                  = 0x84F9;

    public const int RENDERBUFFER_WIDTH             = 0x8D42;
    public const int RENDERBUFFER_HEIGHT            = 0x8D43;
    public const int RENDERBUFFER_INTERNAL_FORMAT   = 0x8D44;
    public const int RENDERBUFFER_RED_SIZE          = 0x8D50;
    public const int RENDERBUFFER_GREEN_SIZE        = 0x8D51;
    public const int RENDERBUFFER_BLUE_SIZE         = 0x8D52;
    public const int RENDERBUFFER_ALPHA_SIZE        = 0x8D53;
    public const int RENDERBUFFER_DEPTH_SIZE        = 0x8D54;
    public const int RENDERBUFFER_STENCIL_SIZE      = 0x8D55;

    public const int FRAMEBUFFER_ATTACHMENT_OBJECT_TYPE           = 0x8CD0;
    public const int FRAMEBUFFER_ATTACHMENT_OBJECT_NAME           = 0x8CD1;
    public const int FRAMEBUFFER_ATTACHMENT_TEXTURE_LEVEL         = 0x8CD2;
    public const int FRAMEBUFFER_ATTACHMENT_TEXTURE_CUBE_MAP_FACE = 0x8CD3;

    public const int COLOR_ATTACHMENT0              = 0x8CE0;
    public const int DEPTH_ATTACHMENT               = 0x8D00;
    public const int STENCIL_ATTACHMENT             = 0x8D20;
    public const int DEPTH_STENCIL_ATTACHMENT       = 0x821A;

    public const int NONE                           = 0;

    public const int FRAMEBUFFER_COMPLETE                      = 0x8CD5;
    public const int FRAMEBUFFER_INCOMPLETE_ATTACHMENT         = 0x8CD6;
    public const int FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT = 0x8CD7;
    public const int FRAMEBUFFER_INCOMPLETE_DIMENSIONS         = 0x8CD9;
    public const int FRAMEBUFFER_UNSUPPORTED                   = 0x8CDD;

    public const int FRAMEBUFFER_BINDING            = 0x8CA6;
    public const int RENDERBUFFER_BINDING           = 0x8CA7;
    public const int MAX_RENDERBUFFER_SIZE          = 0x84E8;

    public const int INVALID_FRAMEBUFFER_OPERATION  = 0x0506;

    /* WebGL-specific enums */
    public const int UNPACK_FLIP_Y_WEBGL            = 0x9240;
    public const int UNPACK_PREMULTIPLY_ALPHA_WEBGL = 0x9241;
    public const int CONTEXT_LOST_WEBGL             = 0x9242;
    public const int UNPACK_COLORSPACE_CONVERSION_WEBGL = 0x9243;
    public const int BROWSER_DEFAULT_WEBGL          = 0x9244;
}
