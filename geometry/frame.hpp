﻿
#pragma once

#include <cstdint>
#include <string>

#include "base/not_constructible.hpp"
#include "geometry/named_quantities.hpp"
#include "google/protobuf/descriptor.h"
#include "serialization/geometry.pb.h"

namespace principia {
namespace geometry {
namespace internal_frame {

using base::not_constructible;
using base::not_null;

enum NonSerializableTag {
  NonSerializable
};

enum Inertia {
  Inertial,
  NonInertial,
};

enum class Handedness {
  Left,
  Right,
};

template<Inertia inertia_ = NonInertial,
         Handedness handedness_ = Handedness::Right,
         typename FrameTag = NonSerializableTag,
         FrameTag tag_ = NonSerializable>
struct Frame : not_constructible {
  static constexpr bool is_inertial = inertia_ == Inertial;
  static constexpr Inertia inertia = inertia_;
  static constexpr Handedness handedness = handedness_;

  static constexpr Position<Frame> const origin;
  static constexpr Velocity<Frame> const unmoving;

  using Tag = FrameTag;
  static constexpr Tag tag = tag_;

  template<typename = std::enable_if<!std::is_same_v<FrameTag,
                                                     NonSerializableTag>>>
  static void WriteToMessage(not_null<serialization::Frame*> message);

  // Checks that the |message| matches the current type.
  template<typename = std::enable_if<!std::is_same_v<FrameTag,
                                                     NonSerializableTag>>>
  static void ReadFromMessage(serialization::Frame const& message);
};

// Extracts enough information from the |message| to contruct a |Frame| type.
void ReadFrameFromMessage(
    serialization::Frame const& message,
    google::protobuf::EnumValueDescriptor const*& enum_value_descriptor,
    bool& is_inertial);

}  // namespace internal_frame

using internal_frame::Frame;
using internal_frame::ReadFrameFromMessage;

}  // namespace geometry
}  // namespace principia

#include "geometry/frame_body.hpp"
