﻿// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: tensorflow/core/lib/core/error_codes.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Tensorflow.Error {

  /// <summary>Holder for reflection information generated from tensorflow/core/lib/core/error_codes.proto</summary>
  public static partial class ErrorCodesReflection {

    #region Descriptor
    /// <summary>File descriptor for tensorflow/core/lib/core/error_codes.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static ErrorCodesReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cip0ZW5zb3JmbG93L2NvcmUvbGliL2NvcmUvZXJyb3JfY29kZXMucHJvdG8S",
            "EHRlbnNvcmZsb3cuZXJyb3IqhAMKBENvZGUSBgoCT0sQABINCglDQU5DRUxM",
            "RUQQARILCgdVTktOT1dOEAISFAoQSU5WQUxJRF9BUkdVTUVOVBADEhUKEURF",
            "QURMSU5FX0VYQ0VFREVEEAQSDQoJTk9UX0ZPVU5EEAUSEgoOQUxSRUFEWV9F",
            "WElTVFMQBhIVChFQRVJNSVNTSU9OX0RFTklFRBAHEhMKD1VOQVVUSEVOVElD",
            "QVRFRBAQEhYKElJFU09VUkNFX0VYSEFVU1RFRBAIEhcKE0ZBSUxFRF9QUkVD",
            "T05ESVRJT04QCRILCgdBQk9SVEVEEAoSEAoMT1VUX09GX1JBTkdFEAsSEQoN",
            "VU5JTVBMRU1FTlRFRBAMEgwKCElOVEVSTkFMEA0SDwoLVU5BVkFJTEFCTEUQ",
            "DhINCglEQVRBX0xPU1MQDxJLCkdET19OT1RfVVNFX1JFU0VSVkVEX0ZPUl9G",
            "VVRVUkVfRVhQQU5TSU9OX1VTRV9ERUZBVUxUX0lOX1NXSVRDSF9JTlNURUFE",
            "XxAUQjEKGG9yZy50ZW5zb3JmbG93LmZyYW1ld29ya0IQRXJyb3JDb2Rlc1By",
            "b3Rvc1AB+AEBYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::Tensorflow.Error.Code), }, null));
    }
    #endregion

  }
  #region Enums
  /// <summary>
  /// The canonical error codes for TensorFlow APIs.
  ///
  /// Warnings:
  ///
  /// -   Do not change any numeric assignments.
  /// -   Changes to this list should only be made if there is a compelling
  ///     need that can't be satisfied in another way.  Such changes
  ///     must be approved by at least two OWNERS.
  ///
  /// Sometimes multiple error codes may apply.  Services should return
  /// the most specific error code that applies.  For example, prefer
  /// OUT_OF_RANGE over FAILED_PRECONDITION if both codes apply.
  /// Similarly prefer NOT_FOUND or ALREADY_EXISTS over FAILED_PRECONDITION.
  /// </summary>
  public enum Code {
    /// <summary>
    /// Not an error; returned on success
    /// </summary>
    [pbr::OriginalName("OK")] Ok = 0,
    /// <summary>
    /// The operation was cancelled (typically by the caller).
    /// </summary>
    [pbr::OriginalName("CANCELLED")] Cancelled = 1,
    /// <summary>
    /// Unknown error.  An example of where this error may be returned is
    /// if a Status value received from another address space belongs to
    /// an error-space that is not known in this address space.  Also
    /// errors raised by APIs that do not return enough error information
    /// may be converted to this error.
    /// </summary>
    [pbr::OriginalName("UNKNOWN")] Unknown = 2,
    /// <summary>
    /// Client specified an invalid argument.  Note that this differs
    /// from FAILED_PRECONDITION.  INVALID_ARGUMENT indicates arguments
    /// that are problematic regardless of the state of the system
    /// (e.g., a malformed file name).
    /// </summary>
    [pbr::OriginalName("INVALID_ARGUMENT")] InvalidArgument = 3,
    /// <summary>
    /// Deadline expired before operation could complete.  For operations
    /// that change the state of the system, this error may be returned
    /// even if the operation has completed successfully.  For example, a
    /// successful response from a server could have been delayed long
    /// enough for the deadline to expire.
    /// </summary>
    [pbr::OriginalName("DEADLINE_EXCEEDED")] DeadlineExceeded = 4,
    /// <summary>
    /// Some requested entity (e.g., file or directory) was not found.
    /// For privacy reasons, this code *may* be returned when the client
    /// does not have the access right to the entity.
    /// </summary>
    [pbr::OriginalName("NOT_FOUND")] NotFound = 5,
    /// <summary>
    /// Some entity that we attempted to create (e.g., file or directory)
    /// already exists.
    /// </summary>
    [pbr::OriginalName("ALREADY_EXISTS")] AlreadyExists = 6,
    /// <summary>
    /// The caller does not have permission to execute the specified
    /// operation.  PERMISSION_DENIED must not be used for rejections
    /// caused by exhausting some resource (use RESOURCE_EXHAUSTED
    /// instead for those errors).  PERMISSION_DENIED must not be
    /// used if the caller can not be identified (use UNAUTHENTICATED
    /// instead for those errors).
    /// </summary>
    [pbr::OriginalName("PERMISSION_DENIED")] PermissionDenied = 7,
    /// <summary>
    /// The request does not have valid authentication credentials for the
    /// operation.
    /// </summary>
    [pbr::OriginalName("UNAUTHENTICATED")] Unauthenticated = 16,
    /// <summary>
    /// Some resource has been exhausted, perhaps a per-user quota, or
    /// perhaps the entire file system is out of space.
    /// </summary>
    [pbr::OriginalName("RESOURCE_EXHAUSTED")] ResourceExhausted = 8,
    /// <summary>
    /// Operation was rejected because the system is not in a state
    /// required for the operation's execution.  For example, directory
    /// to be deleted may be non-empty, an rmdir operation is applied to
    /// a non-directory, etc.
    ///
    /// A litmus test that may help a service implementor in deciding
    /// between FAILED_PRECONDITION, ABORTED, and UNAVAILABLE:
    ///  (a) Use UNAVAILABLE if the client can retry just the failing call.
    ///  (b) Use ABORTED if the client should retry at a higher-level
    ///      (e.g., restarting a read-modify-write sequence).
    ///  (c) Use FAILED_PRECONDITION if the client should not retry until
    ///      the system state has been explicitly fixed.  E.g., if an "rmdir"
    ///      fails because the directory is non-empty, FAILED_PRECONDITION
    ///      should be returned since the client should not retry unless
    ///      they have first fixed up the directory by deleting files from it.
    ///  (d) Use FAILED_PRECONDITION if the client performs conditional
    ///      REST Get/Update/Delete on a resource and the resource on the
    ///      server does not match the condition. E.g., conflicting
    ///      read-modify-write on the same resource.
    /// </summary>
    [pbr::OriginalName("FAILED_PRECONDITION")] FailedPrecondition = 9,
    /// <summary>
    /// The operation was aborted, typically due to a concurrency issue
    /// like sequencer check failures, transaction aborts, etc.
    ///
    /// See litmus test above for deciding between FAILED_PRECONDITION,
    /// ABORTED, and UNAVAILABLE.
    /// </summary>
    [pbr::OriginalName("ABORTED")] Aborted = 10,
    /// <summary>
    /// Operation tried to iterate past the valid input range.  E.g., seeking or
    /// reading past end of file.
    ///
    /// Unlike INVALID_ARGUMENT, this error indicates a problem that may
    /// be fixed if the system state changes. For example, a 32-bit file
    /// system will generate INVALID_ARGUMENT if asked to read at an
    /// offset that is not in the range [0,2^32-1], but it will generate
    /// OUT_OF_RANGE if asked to read from an offset past the current
    /// file size.
    ///
    /// There is a fair bit of overlap between FAILED_PRECONDITION and
    /// OUT_OF_RANGE.  We recommend using OUT_OF_RANGE (the more specific
    /// error) when it applies so that callers who are iterating through
    /// a space can easily look for an OUT_OF_RANGE error to detect when
    /// they are done.
    /// </summary>
    [pbr::OriginalName("OUT_OF_RANGE")] OutOfRange = 11,
    /// <summary>
    /// Operation is not implemented or not supported/enabled in this service.
    /// </summary>
    [pbr::OriginalName("UNIMPLEMENTED")] Unimplemented = 12,
    /// <summary>
    /// Internal errors.  Means some invariants expected by underlying
    /// system has been broken.  If you see one of these errors,
    /// something is very broken.
    /// </summary>
    [pbr::OriginalName("INTERNAL")] Internal = 13,
    /// <summary>
    /// The service is currently unavailable.  This is a most likely a
    /// transient condition and may be corrected by retrying with
    /// a backoff.
    ///
    /// See litmus test above for deciding between FAILED_PRECONDITION,
    /// ABORTED, and UNAVAILABLE.
    /// </summary>
    [pbr::OriginalName("UNAVAILABLE")] Unavailable = 14,
    /// <summary>
    /// Unrecoverable data loss or corruption.
    /// </summary>
    [pbr::OriginalName("DATA_LOSS")] DataLoss = 15,
    /// <summary>
    /// An extra enum entry to prevent people from writing code that
    /// fails to compile when a new code is added.
    ///
    /// Nobody should ever reference this enumeration entry. In particular,
    /// if you write C++ code that switches on this enumeration, add a default:
    /// case instead of a case that mentions this enumeration entry.
    ///
    /// Nobody should rely on the value (currently 20) listed here.  It
    /// may change in the future.
    /// </summary>
    [pbr::OriginalName("DO_NOT_USE_RESERVED_FOR_FUTURE_EXPANSION_USE_DEFAULT_IN_SWITCH_INSTEAD_")] DoNotUseReservedForFutureExpansionUseDefaultInSwitchInstead = 20,
  }

  #endregion

}

#endregion Designer generated code
