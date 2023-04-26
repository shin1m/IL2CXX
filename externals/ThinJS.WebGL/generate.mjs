import { open } from 'node:fs/promises';

const webgl = `
void activeTexture(int texture);
void attachShader(JSObject program, JSObject shader);
void bindAttribLocation(JSObject program, int index, string name);
void bindBuffer(int target, JSObject? buffer);
void bindFramebuffer(int target, JSObject? framebuffer);
void bindRenderbuffer(int target, JSObject? renderbuffer);
void bindTexture(int target, JSObject? texture);
void blendColor(float red, float green, float blue, float alpha);
void blendEquation(int mode);
void blendEquationSeparate(int modeRGB, int modeAlpha);
void blendFunc(int sfactor, int dfactor);
void blendFuncSeparate(int srcRGB, int dstRGB, int srcAlpha, int dstAlpha);

int checkFramebufferStatus(int target);
void clear(int mask);
void clearColor(float red, float green, float blue, float alpha);
void clearDepth(float depth);
void clearStencil(int s);
void colorMask(bool red, bool green, bool blue, bool alpha);
void compileShader(JSObject shader);

void copyTexImage2D(int target, int level, int internalformat, int x, int y, int width, int height, int border);
void copyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height);

JSObject? createBuffer();
JSObject? createFramebuffer();
JSObject? createProgram();
JSObject? createRenderbuffer();
JSObject? createShader(int type);
JSObject? createTexture();

void cullFace(int mode);

void deleteBuffer(JSObject? buffer);
void deleteFramebuffer(JSObject? framebuffer);
void deleteProgram(JSObject? program);
void deleteRenderbuffer(JSObject? renderbuffer);
void deleteShader(JSObject? shader);
void deleteTexture(JSObject? texture);

void depthFunc(int func);
void depthMask(bool flag);
void depthRange(float zNear, float zFar);
void detachShader(JSObject program, JSObject shader);
void disable(int cap);
void disableVertexAttribArray(int index);
void drawArrays(int mode, int first, int count);
void drawElements(int mode, int count, int type, long offset);

void enable(int cap);
void enableVertexAttribArray(int index);
void finish();
void flush();
void framebufferRenderbuffer(int target, int attachment, int renderbuffertarget, JSObject? renderbuffer);
void framebufferTexture2D(int target, int attachment, int textarget, JSObject? texture, int level);
void frontFace(int mode);

void generateMipmap(int target);

JSObject? getActiveAttrib(JSObject program, int index);
JSObject? getActiveUniform(JSObject program, int index);
JSObject[]? getAttachedShaders(JSObject program);

int getAttribLocation(JSObject program, string name);

object getBufferParameter(int target, int pname);
object getParameter(int pname);

int getError();

object getFramebufferAttachmentParameter(int target, int attachment, int pname);
object getProgramParameter(JSObject program, int pname);
string? getProgramInfoLog(JSObject program);
object getRenderbufferParameter(int target, int pname);
object getShaderParameter(JSObject shader, int pname);
JSObject? getShaderPrecisionFormat(int shadertype, int precisiontype);
string? getShaderInfoLog(JSObject shader);

string? getShaderSource(JSObject shader);

object getTexParameter(int target, int pname);

object getUniform(JSObject program, JSObject location);

JSObject? getUniformLocation(JSObject program, string name);

object getVertexAttrib(int index, int pname);

long getVertexAttribOffset(int index, int pname);

void hint(int target, int mode);
bool isBuffer(JSObject? buffer);
bool isEnabled(int cap);
bool isFramebuffer(JSObject? framebuffer);
bool isProgram(JSObject? program);
bool isRenderbuffer(JSObject? renderbuffer);
bool isShader(JSObject? shader);
bool isTexture(JSObject? texture);
void lineWidth(float width);
void linkProgram(JSObject program);
void pixelStorei(int pname, int param);
void polygonOffset(float factor, float units);

void renderbufferStorage(int target, int internalformat, int width, int height);
void sampleCoverage(float value, bool invert);
void scissor(int x, int y, int width, int height);

void shaderSource(JSObject shader, string source);

void stencilFunc(int func, int @ref, int mask);
void stencilFuncSeparate(int face, int func, int @ref, int mask);
void stencilMask(int mask);
void stencilMaskSeparate(int face, int mask);
void stencilOp(int fail, int zfail, int zpass);
void stencilOpSeparate(int face, int fail, int zfail, int zpass);

void texParameterf(int target, int pname, float param);
void texParameteri(int target, int pname, int param);

void uniform1f(JSObject? location, float x);
void uniform2f(JSObject? location, float x, float y);
void uniform3f(JSObject? location, float x, float y, float z);
void uniform4f(JSObject? location, float x, float y, float z, float w);

void uniform1i(JSObject? location, int x);
void uniform2i(JSObject? location, int x, int y);
void uniform3i(JSObject? location, int x, int y, int z);
void uniform4i(JSObject? location, int x, int y, int z, int w);

void useProgram(JSObject? program);
void validateProgram(JSObject program);

void vertexAttrib1f(int index, float x);
void vertexAttrib2f(int index, float x, float y);
void vertexAttrib3f(int index, float x, float y, float z);
void vertexAttrib4f(int index, float x, float y, float z, float w);

void vertexAttrib1fv(int index, Span<float> values);
void vertexAttrib2fv(int index, Span<float> values);
void vertexAttrib3fv(int index, Span<float> values);
void vertexAttrib4fv(int index, Span<float> values);

void vertexAttribPointer(int index, int size, int type, bool normalized, int stride, long offset);

void viewport(int x, int y, int width, int height);

void bufferData(int target, long size, int usage);
void bufferData<T>(int target, Span<T> data, int usage);
void bufferSubData<T>(int target, long offset, Span<T> data);

void compressedTexImage2D<T>(int target, int level, int internalformat, int width, int height, int border, Span<T> data);
void compressedTexSubImage2D<T>(int target, int level, int xoffset, int yoffset, int width, int height, int format, Span<T> data);

void readPixels(int x, int y, int width, int height, int format, int type, JSObject? pixels);

void texImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type);
void texImage2D<T>(int target, int level, int internalformat, int width, int height, int border, int format, int type, Span<T> pixels);
void texImage2D(int target, int level, int internalformat, int format, int type, JSObject source);

void texSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type);
void texSubImage2D<T>(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, Span<T> pixels);
void texSubImage2D(int target, int level, int xoffset, int yoffset, int format, int type, JSObject source);

void uniform1fv(JSObject? location, Span<float> v);
void uniform2fv(JSObject? location, Span<float> v);
void uniform3fv(JSObject? location, Span<float> v);
void uniform4fv(JSObject? location, Span<float> v);

void uniform1iv(JSObject? location, Span<int> v);
void uniform2iv(JSObject? location, Span<int> v);
void uniform3iv(JSObject? location, Span<int> v);
void uniform4iv(JSObject? location, Span<int> v);

void uniformMatrix2fv(JSObject? location, bool transpose, Span<float> value);
void uniformMatrix3fv(JSObject? location, bool transpose, Span<float> value);
void uniformMatrix4fv(JSObject? location, bool transpose, Span<float> value);
`;

const webgl2 = `
void copyBufferSubData(int readTarget, int writeTarget, long readOffset, long writeOffset, long size);
void getBufferSubData<T>(int target, long srcByteOffset, Span<T> dstBuffer);
void getBufferSubData<T>(int target, long srcByteOffset, Span<T> dstBuffer, int dstOffset);
void getBufferSubData<T>(int target, long srcByteOffset, Span<T> dstBuffer, int dstOffset, int length);

void blitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, int mask, int filter);
void framebufferTextureLayer(int target, int attachment, JSObject? texture, int level, int layer);
void invalidateFramebuffer(int target, Span<int> attachments);
void invalidateSubFramebuffer(int target, Span<int> attachments, int x, int y, int width, int height);
void readBuffer(int src);

object getInternalformatParameter(int target, int internalformat, int pname);
void renderbufferStorageMultisample(int target, int samples, int internalformat, int width, int height);

void texStorage2D(int target, int levels, int internalformat, int width, int height);
void texStorage3D(int target, int levels, int internalformat, int width, int height, int depth);

void texImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, long pboOffset);
void texImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, JSObject source);
void texImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type);
void texImage3D<T>(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, Span<T> srcData);
void texImage3D<T>(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, Span<T> srcData, int srcOffset);

void texSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, long pboOffset);
void texSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, JSObject source);
void texSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type);
void texSubImage3D<T>(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, Span<T> srcData);
void texSubImage3D<T>(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int type, Span<T> srcData, int srcOffset);

void copyTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height);

void compressedTexImage3D(int target, int level, int internalformat, int width, int height, int depth, int border, int imageSize, long offset);
void compressedTexImage3D<T>(int target, int level, int internalformat, int width, int height, int depth, int border, Span<T> srcData);
void compressedTexImage3D<T>(int target, int level, int internalformat, int width, int height, int depth, int border, Span<T> srcData, int srcOffset);
void compressedTexImage3D<T>(int target, int level, int internalformat, int width, int height, int depth, int border, Span<T> srcData, int srcOffset, int srcLengthOverride);

void compressedTexSubImage3D(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, int imageSize, long offset);
void compressedTexSubImage3D<T>(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, Span<T> srcData);
void compressedTexSubImage3D<T>(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, Span<T> srcData, int srcOffset);
void compressedTexSubImage3D<T>(int target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, int format, Span<T> srcData, int srcOffset, int srcLengthOverride);

int getFragDataLocation(JSObject program, string name);

void uniform1ui(JSObject? location, int v0);
void uniform2ui(JSObject? location, int v0, int v1);
void uniform3ui(JSObject? location, int v0, int v1, int v2);
void uniform4ui(JSObject? location, int v0, int v1, int v2, int v3);

void uniform1uiv(JSObject? location, Span<uint> data);
void uniform1uiv(JSObject? location, Span<uint> data, int srcOffset);
void uniform1uiv(JSObject? location, Span<uint> data, int srcOffset, int srcLength);
void uniform2uiv(JSObject? location, Span<uint> data);
void uniform2uiv(JSObject? location, Span<uint> data, int srcOffset);
void uniform2uiv(JSObject? location, Span<uint> data, int srcOffset, int srcLength);
void uniform3uiv(JSObject? location, Span<uint> data);
void uniform3uiv(JSObject? location, Span<uint> data, int srcOffset);
void uniform3uiv(JSObject? location, Span<uint> data, int srcOffset, int srcLength);
void uniform4uiv(JSObject? location, Span<uint> data);
void uniform4uiv(JSObject? location, Span<uint> data, int srcOffset);
void uniform4uiv(JSObject? location, Span<uint> data, int srcOffset, int srcLength);
void uniformMatrix3x2fv(JSObject? location, bool transpose, Span<float> data);
void uniformMatrix3x2fv(JSObject? location, bool transpose, Span<float> data, int srcOffset);
void uniformMatrix3x2fv(JSObject? location, bool transpose, Span<float> data, int srcOffset, int srcLength);
void uniformMatrix4x2fv(JSObject? location, bool transpose, Span<float> data);
void uniformMatrix4x2fv(JSObject? location, bool transpose, Span<float> data, int srcOffset);
void uniformMatrix4x2fv(JSObject? location, bool transpose, Span<float> data, int srcOffset, int srcLength);

void uniformMatrix2x3fv(JSObject? location, bool transpose, Span<float> data);
void uniformMatrix2x3fv(JSObject? location, bool transpose, Span<float> data, int srcOffset);
void uniformMatrix2x3fv(JSObject? location, bool transpose, Span<float> data, int srcOffset, int srcLength);
void uniformMatrix4x3fv(JSObject? location, bool transpose, Span<float> data);
void uniformMatrix4x3fv(JSObject? location, bool transpose, Span<float> data, int srcOffset);
void uniformMatrix4x3fv(JSObject? location, bool transpose, Span<float> data, int srcOffset, int srcLength);

void uniformMatrix2x4fv(JSObject? location, bool transpose, Span<float> data);
void uniformMatrix2x4fv(JSObject? location, bool transpose, Span<float> data, int srcOffset);
void uniformMatrix2x4fv(JSObject? location, bool transpose, Span<float> data, int srcOffset, int srcLength);
void uniformMatrix3x4fv(JSObject? location, bool transpose, Span<float> data);
void uniformMatrix3x4fv(JSObject? location, bool transpose, Span<float> data, int srcOffset);
void uniformMatrix3x4fv(JSObject? location, bool transpose, Span<float> data, int srcOffset, int srcLength);

void vertexAttribI4i(int index, int x, int y, int z, int w);
void vertexAttribI4iv(int index, Span<int> values);
void vertexAttribI4ui(int index, int x, int y, int z, int w);
void vertexAttribI4uiv(int index, Span<uint> values);
void vertexAttribIPointer(int index, int size, int type, int stride, long offset);

void vertexAttribDivisor(int index, int divisor);
void drawArraysInstanced(int mode, int first, int count, int instanceCount);
void drawElementsInstanced(int mode, int count, int type, long offset, int instanceCount);
void drawRangeElements(int mode, int start, int end, int count, int type, long offset);

void drawBuffers(Span<int> buffers);

void clearBufferfv(int buffer, int drawbuffer, Span<float> values);
void clearBufferfv(int buffer, int drawbuffer, Span<float> values, int srcOffset);
void clearBufferiv(int buffer, int drawbuffer, Span<int> values);
void clearBufferiv(int buffer, int drawbuffer, Span<int> values, int srcOffset);
void clearBufferuiv(int buffer, int drawbuffer, Span<uint> values);
void clearBufferuiv(int buffer, int drawbuffer, Span<uint> values, int srcOffset);

void clearBufferfi(int buffer, int drawbuffer, float depth, int stencil);

JSObject? createQuery();
void deleteQuery(JSObject? query);
bool isQuery(JSObject? query);
void beginQuery(int target, JSObject query);
void endQuery(int target);
JSObject? getQuery(int target, int pname);
object getQueryParameter(JSObject query, int pname);

JSObject? createSampler();
void deleteSampler(JSObject? sampler);
bool isSampler(JSObject? sampler);
void bindSampler(int unit, JSObject? sampler);
void samplerParameteri(JSObject sampler, int pname, int param);
void samplerParameterf(JSObject sampler, int pname, float param);
object getSamplerParameter(JSObject sampler, int pname);

JSObject? fenceSync(int condition, int flags);
bool isSync(JSObject? sync);
void deleteSync(JSObject? sync);
int clientWaitSync(JSObject sync, int flags, long timeout);
void waitSync(JSObject sync, int flags, long timeout);
object getSyncParameter(JSObject sync, int pname);

JSObject? createTransformFeedback();
void deleteTransformFeedback(JSObject? tf);
bool isTransformFeedback(JSObject? tf);
void bindTransformFeedback(int target, JSObject? tf);
void beginTransformFeedback(int primitiveMode);
void endTransformFeedback();
void transformFeedbackVaryings(JSObject program, string[] varyings, int bufferMode);
JSObject? getTransformFeedbackVarying(JSObject program, int index);
void pauseTransformFeedback();
void resumeTransformFeedback();

void bindBufferBase(int target, int index, JSObject? buffer);
void bindBufferRange(int target, int index, JSObject? buffer, long offset, long size);
object getIndexedParameter(int target, int index);
int[]? getUniformIndices(JSObject program, string[] uniformNames);
object getActiveUniforms(JSObject program, Span<int> uniformIndices, int pname);
int getUniformBlockIndex(JSObject program, string uniformBlockName);
object getActiveUniformBlockParameter(JSObject program, int uniformBlockIndex, int pname);
string? getActiveUniformBlockName(JSObject program, int uniformBlockIndex);
void uniformBlockBinding(JSObject program, int uniformBlockIndex, int uniformBlockBinding);

JSObject? createVertexArray();
void deleteVertexArray(JSObject? vertexArray);
bool isVertexArray(JSObject? vertexArray);
void bindVertexArray(JSObject? array);

void bufferData<T>(int target, Span<T> srcData, int usage, int srcOffset);
void bufferData<T>(int target, Span<T> srcData, int usage, int srcOffset, int length);
void bufferSubData<T>(int target, long dstByteOffset, Span<T> srcData, int srcOffset);
void bufferSubData<T>(int target, long dstByteOffset, Span<T> srcData, int srcOffset, int length);

void texImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, long pboOffset);
void texImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, JSObject source);
void texImage2D<T>(int target, int level, int internalformat, int width, int height, int border, int format, int type, Span<T> srcData, int srcOffset);

void texSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, long pboOffset);
void texSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, JSObject source);
void texSubImage2D<T>(int target, int level, int xoffset, int yoffset, int width, int height, int format, int type, Span<T> srcData, int srcOffset);

void compressedTexImage2D(int target, int level, int internalformat, int width, int height, int border, int imageSize, long offset);
void /*compressedTexImage2D<T>*/(int target, int level, int internalformat, int width, int height, int border, Span<T> srcData);
void compressedTexImage2D<T>(int target, int level, int internalformat, int width, int height, int border, Span<T> srcData, int srcOffset);
void compressedTexImage2D<T>(int target, int level, int internalformat, int width, int height, int border, Span<T> srcData, int srcOffset, int srcLengthOverride);

void compressedTexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, int format, int imageSize, long offset);
void /*compressedTexSubImage2D<T>*/(int target, int level, int xoffset, int yoffset, int width, int height, int format, Span<T> srcData);
void compressedTexSubImage2D<T>(int target, int level, int xoffset, int yoffset, int width, int height, int format, Span<T> srcData, int srcOffset);
void compressedTexSubImage2D<T>(int target, int level, int xoffset, int yoffset, int width, int height, int format, Span<T> srcData, int srcOffset, int srcLengthOverride);

void /*uniform1fv*/(JSObject? location, Span<float> data);
void uniform1fv(JSObject? location, Span<float> data, int srcOffset);
void uniform1fv(JSObject? location, Span<float> data, int srcOffset, int srcLength);
void /*uniform2fv*/(JSObject? location, Span<float> data);
void uniform2fv(JSObject? location, Span<float> data, int srcOffset);
void uniform2fv(JSObject? location, Span<float> data, int srcOffset, int srcLength);
void /*uniform3fv*/(JSObject? location, Span<float> data);
void uniform3fv(JSObject? location, Span<float> data, int srcOffset);
void uniform3fv(JSObject? location, Span<float> data, int srcOffset, int srcLength);
void /*uniform4fv*/(JSObject? location, Span<float> data);
void uniform4fv(JSObject? location, Span<float> data, int srcOffset);
void uniform4fv(JSObject? location, Span<float> data, int srcOffset, int srcLength);

void /*uniform1iv*/(JSObject? location, Span<int> data);
void uniform1iv(JSObject? location, Span<int> data, int srcOffset);
void uniform1iv(JSObject? location, Span<int> data, int srcOffset, int srcLength);
void /*uniform2iv*/(JSObject? location, Span<int> data);
void uniform2iv(JSObject? location, Span<int> data, int srcOffset);
void uniform2iv(JSObject? location, Span<int> data, int srcOffset, int srcLength);
void /*uniform3iv*/(JSObject? location, Span<int> data);
void uniform3iv(JSObject? location, Span<int> data, int srcOffset);
void uniform3iv(JSObject? location, Span<int> data, int srcOffset, int srcLength);
void /*uniform4iv*/(JSObject? location, Span<int> data);
void uniform4iv(JSObject? location, Span<int> data, int srcOffset);
void uniform4iv(JSObject? location, Span<int> data, int srcOffset, int srcLength);

void /*uniformMatrix2fv*/(JSObject? location, bool transpose, Span<float> data);
void uniformMatrix2fv(JSObject? location, bool transpose, Span<float> data, int srcOffset);
void uniformMatrix2fv(JSObject? location, bool transpose, Span<float> data, int srcOffset, int srcLength);
void /*uniformMatrix3fv*/(JSObject? location, bool transpose, Span<float> data);
void uniformMatrix3fv(JSObject? location, bool transpose, Span<float> data, int srcOffset);
void uniformMatrix3fv(JSObject? location, bool transpose, Span<float> data, int srcOffset, int srcLength);
void /*uniformMatrix4fv*/(JSObject? location, bool transpose, Span<float> data);
void uniformMatrix4fv(JSObject? location, bool transpose, Span<float> data, int srcOffset);
void uniformMatrix4fv(JSObject? location, bool transpose, Span<float> data, int srcOffset, int srcLength);

void readPixels(int x, int y, int width, int height, int format, int type, long offset);
void readPixels(int x, int y, int width, int height, int format, int type, Span<byte> dstData, int dstOffset);
void readPixels(int x, int y, int width, int height, int format, int type, Span<ushort> dstData, int dstOffset);
void readPixels(int x, int y, int width, int height, int format, int type, Span<float> dstData, int dstOffset);
`;

async function write(path, action) {
	const f = await open(path, 'w');
	try {
		await action(f);
	} finally {
		await f.close();
	}
}

const out = process.argv[2];
await write(`${out}WebGL.g.cs`, async cs =>
await write(`${out}WebGL2.g.cs`, async cs2 =>
await write(`${out}thinjs.webgl.js`, async js => {
	await cs.write(`using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

namespace ThinJS;

#nullable enable

partial class WebGL : IDisposable
{
    protected readonly JSObject context;

    protected unsafe Span<byte> AsBytes<T>(Span<T> data) where T : unmanaged => new Span<byte>(Unsafe.AsPointer(ref data[0]), data.Length * sizeof(T));

    [JSImport("create", "thisjs.webgl")]
    private static partial JSObject Create(JSObject canvas, string name);
    protected WebGL(JSObject canvas, string name) => context = Create(canvas, name);
    public WebGL(JSObject canvas) : this(canvas, "webgl") { }
    public void Dispose() => context.Dispose();
    [JSImport("width", "thisjs.webgl")]
    private static partial float GetWidth(JSObject context);
    public float Width => GetWidth(context);
    [JSImport("height", "thisjs.webgl")]
    private static partial float GetHeight(JSObject context);
    public float Height => GetHeight(context);`);
	await cs2.write(`using System.Numerics;
using System.Runtime.InteropServices.JavaScript;

namespace ThinJS;

#nullable enable

partial class WebGL2 : WebGL
{
    public WebGL2(JSObject canvas) : base(canvas, "webgl2") { }
`);
	await js.write(`import { dotnet } from './dotnet.js';
export function webgl(set) {
    const module = dotnet.instance.Module;
    const asArray = (type, bytes) => new type(module.HEAPU8.buffer, bytes._pointer, bytes._length / type.BYTES_PER_ELEMENT);
    set('thisjs.webgl', {
        create: (canvas, name) => canvas.getContext(name),
        width: gl => gl.canvas.clientWidth,
        height: gl => gl.canvas.clientHeight`);
	const pascal = x => x[0].toUpperCase() + x.slice(1);
	const join = xs => xs.join(', ');
	const span = type => /^Span<(\w+)>$/.exec(type)?.[1];
	const marshal = type =>
		type === 'long' ? '[JSMarshalAs<JSType.Number>]long' :
		span(type) ? '[JSMarshalAs<JSType.MemoryView>]Span<byte>' :
		type;
	const cast = ({type, name}) => span(type) ? `AsBytes(${name})` : name;
	const types = {
		ushort: 'Uint16',
		int: 'Int32',
		uint: 'Uint32',
		float: 'Float32'
	};
	const unmarshal = ({type, name}) => {
		const t = span(type);
		return t ? `asArray(${types[t] ?? 'Uint8'}Array, ${name})` : name;
	};
	const jsnames = new Set();
	async function generate(source, cs) {
		for (const {groups: {type, name, generic, parameters}} of source.matchAll(
			/(?<type>\S+)\s+(?<name>\w+)(?<generic><\w+>)?\((?<parameters>[^)]*)\)\s*;/g
		)) {
			const pairs = Array.from(parameters.matchAll(/\s*(?<type>[^ ,]+)\s+(?<name>\w+)/g), ({groups}) => groups);
			let jsname = name;
			for (let i = 0; jsnames.has(jsname); ++i) jsname = `${name}__${i}`;
			jsnames.add(jsname);
			const csname = pascal(jsname);
			await cs.write(`
    [JSImport("${jsname}", "thisjs.webgl")]${
	type === 'object' ? '[return: JSMarshalAs<JSType.Any>]' :
	type === 'long' ? '[return: JSMarshalAs<JSType.Number>]' :
	''}
    private static partial ${type} ${csname}(${join(['JSObject context', ...pairs.map(({type, name}) => `${marshal(type)} ${name}`)])});
    public ${type} ${pascal(name)}${generic ?? ''}(${join(pairs.map(({type, name}) => `${type} ${name}`))})${generic ? ` where ${generic.slice(1, -1)} : unmanaged` : ''} => ${csname}(${join(['context', ...pairs.map(cast)])});`);
			await js.write(`,
        ${jsname}: (${join(['gl', ...pairs.map(({name}) => name)])}) => gl.${name}(${join(pairs.map(unmarshal))})`);
		}
	}
	await generate(webgl, cs);
	await generate(webgl2, cs2);
	await cs.write(`
    public unsafe void UniformMatrix4fv(JSObject? location, bool transpose, Matrix4x4 value) => UniformMatrix4fv(context, location, transpose, new Span<byte>(&value, sizeof(Matrix4x4)));
}
`);
	await cs2.write(`
}
`);
	await js.write(`
    });
}
`);
})));
