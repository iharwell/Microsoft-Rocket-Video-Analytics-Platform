﻿// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: tensorflow_serving/apis/model.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Tensorflow.Serving {

  /// <summary>Holder for reflection information generated from tensorflow_serving/apis/model.proto</summary>
  public static partial class ModelReflection {

    #region Descriptor
    /// <summary>File descriptor for tensorflow_serving/apis/model.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static ModelReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CiN0ZW5zb3JmbG93X3NlcnZpbmcvYXBpcy9tb2RlbC5wcm90bxISdGVuc29y",
            "Zmxvdy5zZXJ2aW5nGh5nb29nbGUvcHJvdG9idWYvd3JhcHBlcnMucHJvdG8i",
            "XwoJTW9kZWxTcGVjEgwKBG5hbWUYASABKAkSLAoHdmVyc2lvbhgCIAEoCzIb",
            "Lmdvb2dsZS5wcm90b2J1Zi5JbnQ2NFZhbHVlEhYKDnNpZ25hdHVyZV9uYW1l",
            "GAMgASgJQgP4AQFiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Google.Protobuf.WellKnownTypes.WrappersReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Tensorflow.Serving.ModelSpec), global::Tensorflow.Serving.ModelSpec.Parser, new[]{ "Name", "Version", "SignatureName" }, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  /// <summary>
  /// Metadata for an inference request such as the model name and version.
  /// </summary>
  public sealed partial class ModelSpec : pb::IMessage<ModelSpec> {
    private static readonly pb::MessageParser<ModelSpec> _parser = new pb::MessageParser<ModelSpec>(() => new ModelSpec());
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<ModelSpec> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Tensorflow.Serving.ModelReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ModelSpec() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ModelSpec(ModelSpec other) : this() {
      name_ = other.name_;
      Version = other.Version;
      signatureName_ = other.signatureName_;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public ModelSpec Clone() {
      return new ModelSpec(this);
    }

    /// <summary>Field number for the "name" field.</summary>
    public const int NameFieldNumber = 1;
    private string name_ = "";
    /// <summary>
    /// Required servable name.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string Name {
      get { return name_; }
      set {
        name_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    /// <summary>Field number for the "version" field.</summary>
    public const int VersionFieldNumber = 2;
    private static readonly pb::FieldCodec<long?> _single_version_codec = pb::FieldCodec.ForStructWrapper<long>(18);
    private long? version_;
    /// <summary>
    /// Optional version. If unspecified, will use the latest (numerical) version.
    /// Typically not needed unless coordinating across multiple models that were
    /// co-trained and/or have inter-dependencies on the versions used at inference
    /// time.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public long? Version {
      get { return version_; }
      set {
        version_ = value;
      }
    }

    /// <summary>Field number for the "signature_name" field.</summary>
    public const int SignatureNameFieldNumber = 3;
    private string signatureName_ = "";
    /// <summary>
    /// A named signature to evaluate. If unspecified, the default signature will
    /// be used.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public string SignatureName {
      get { return signatureName_; }
      set {
        signatureName_ = pb::ProtoPreconditions.CheckNotNull(value, "value");
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as ModelSpec);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(ModelSpec other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (Name != other.Name) return false;
      if (Version != other.Version) return false;
      if (SignatureName != other.SignatureName) return false;
      return true;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (Name.Length != 0) hash ^= Name.GetHashCode();
      if (version_ != null) hash ^= Version.GetHashCode();
      if (SignatureName.Length != 0) hash ^= SignatureName.GetHashCode();
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (Name.Length != 0) {
        output.WriteRawTag(10);
        output.WriteString(Name);
      }
      if (version_ != null) {
        _single_version_codec.WriteTagAndValue(output, Version);
      }
      if (SignatureName.Length != 0) {
        output.WriteRawTag(26);
        output.WriteString(SignatureName);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (Name.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(Name);
      }
      if (version_ != null) {
        size += _single_version_codec.CalculateSizeWithTag(Version);
      }
      if (SignatureName.Length != 0) {
        size += 1 + pb::CodedOutputStream.ComputeStringSize(SignatureName);
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(ModelSpec other) {
      if (other == null) {
        return;
      }
      if (other.Name.Length != 0) {
        Name = other.Name;
      }
      if (other.version_ != null) {
        if (version_ == null || other.Version != 0L) {
          Version = other.Version;
        }
      }
      if (other.SignatureName.Length != 0) {
        SignatureName = other.SignatureName;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            input.SkipLastField();
            break;
          case 10: {
            Name = input.ReadString();
            break;
          }
          case 18: {
            long? value = _single_version_codec.Read(input);
            if (version_ == null || value != 0L) {
              Version = value;
            }
            break;
          }
          case 26: {
            SignatureName = input.ReadString();
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
