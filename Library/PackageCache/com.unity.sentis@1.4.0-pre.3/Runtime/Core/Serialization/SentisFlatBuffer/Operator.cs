// <auto-generated>
//  automatically generated by the FlatBuffers compiler, do not modify
// </auto-generated>
#define ENABLE_SPAN_T
#define UNSAFE_BYTEBUFFER
#define BYTEBUFFER_NO_BOUNDS_CHECK

namespace SentisFlatBuffer
{

using global::System;
using global::System.Collections.Generic;
using global::Unity.Sentis.Google.FlatBuffers;

struct Operator : IFlatbufferObject
{
  private Table __p;
  public ByteBuffer ByteBuffer { get { return __p.bb; } }
  public static void ValidateVersion() { FlatBufferConstants.FLATBUFFERS_23_5_26(); }
  public static Operator GetRootAsOperator(ByteBuffer _bb) { return GetRootAsOperator(_bb, new Operator()); }
  public static Operator GetRootAsOperator(ByteBuffer _bb, Operator obj) { return (obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb)); }
  public void __init(int _i, ByteBuffer _bb) { __p = new Table(_i, _bb); }
  public Operator __assign(int _i, ByteBuffer _bb) { __init(_i, _bb); return this; }

  public string Name { get { int o = __p.__offset(4); return o != 0 ? __p.__string(o + __p.bb_pos) : null; } }
#if ENABLE_SPAN_T
  public Span<byte> GetNameBytes() { return __p.__vector_as_span<byte>(4, 1); }
#else
  public ArraySegment<byte>? GetNameBytes() { return __p.__vector_as_arraysegment(4); }
#endif
  public byte[] GetNameArray() { return __p.__vector_as_array<byte>(4); }

  public static Offset<SentisFlatBuffer.Operator> CreateOperator(FlatBufferBuilder builder,
      StringOffset nameOffset = default(StringOffset)) {
    builder.StartTable(1);
    Operator.AddName(builder, nameOffset);
    return Operator.EndOperator(builder);
  }

  public static void StartOperator(FlatBufferBuilder builder) { builder.StartTable(1); }
  public static void AddName(FlatBufferBuilder builder, StringOffset nameOffset) { builder.AddOffset(0, nameOffset.Value, 0); }
  public static Offset<SentisFlatBuffer.Operator> EndOperator(FlatBufferBuilder builder) {
    int o = builder.EndTable();
    return new Offset<SentisFlatBuffer.Operator>(o);
  }
}


static class OperatorVerify
{
  static public bool Verify(Unity.Sentis.Google.FlatBuffers.Verifier verifier, uint tablePos)
  {
    return verifier.VerifyTableStart(tablePos)
      && verifier.VerifyString(tablePos, 4 /*Name*/, false)
      && verifier.VerifyTableEnd(tablePos);
  }
}

}
