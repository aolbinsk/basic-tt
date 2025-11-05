#pragma once

#include "Vector3.h"
#include <cmath>

namespace BasicTT {

struct Quaternion {
    float x, y, z, w;

    Quaternion() : x(0.0f), y(0.0f), z(0.0f), w(1.0f) {}
    Quaternion(float x, float y, float z, float w) : x(x), y(y), z(z), w(w) {}

    // Quaternion operations
    Quaternion operator*(const Quaternion& other) const {
        return Quaternion(
            w * other.x + x * other.w + y * other.z - z * other.y,
            w * other.y - x * other.z + y * other.w + z * other.x,
            w * other.z + x * other.y - y * other.x + z * other.w,
            w * other.w - x * other.x - y * other.y - z * other.z
        );
    }

    Vector3 operator*(const Vector3& v) const {
        // Rotate vector by quaternion
        Vector3 qvec(x, y, z);
        Vector3 uv = Vector3::Cross(qvec, v);
        Vector3 uuv = Vector3::Cross(qvec, uv);
        return v + (uv * w + uuv) * 2.0f;
    }

    Quaternion Conjugate() const {
        return Quaternion(-x, -y, -z, w);
    }

    float Magnitude() const {
        return std::sqrt(x * x + y * y + z * z + w * w);
    }

    Quaternion Normalized() const {
        float mag = Magnitude();
        if (mag > 1e-6f) {
            return Quaternion(x / mag, y / mag, z / mag, w / mag);
        }
        return Identity();
    }

    void Normalize() {
        float mag = Magnitude();
        if (mag > 1e-6f) {
            x /= mag;
            y /= mag;
            z /= mag;
            w /= mag;
        }
    }

    // Convert to Euler angles (in radians)
    Vector3 ToEulerAngles() const {
        Vector3 angles;

        // Roll (x-axis rotation)
        float sinr_cosp = 2.0f * (w * x + y * z);
        float cosr_cosp = 1.0f - 2.0f * (x * x + y * y);
        angles.x = std::atan2(sinr_cosp, cosr_cosp);

        // Pitch (y-axis rotation)
        float sinp = 2.0f * (w * y - z * x);
        if (std::abs(sinp) >= 1.0f)
            angles.y = std::copysign(M_PI / 2.0f, sinp);
        else
            angles.y = std::asin(sinp);

        // Yaw (z-axis rotation)
        float siny_cosp = 2.0f * (w * z + x * y);
        float cosy_cosp = 1.0f - 2.0f * (y * y + z * z);
        angles.z = std::atan2(siny_cosp, cosy_cosp);

        return angles;
    }

    // Static methods
    static Quaternion Identity() {
        return Quaternion(0, 0, 0, 1);
    }

    static Quaternion FromEuler(float x, float y, float z) {
        float cx = std::cos(x * 0.5f);
        float sx = std::sin(x * 0.5f);
        float cy = std::cos(y * 0.5f);
        float sy = std::sin(y * 0.5f);
        float cz = std::cos(z * 0.5f);
        float sz = std::sin(z * 0.5f);

        return Quaternion(
            sx * cy * cz - cx * sy * sz,
            cx * sy * cz + sx * cy * sz,
            cx * cy * sz - sx * sy * cz,
            cx * cy * cz + sx * sy * sz
        );
    }

    static Quaternion FromAxisAngle(const Vector3& axis, float angle) {
        float halfAngle = angle * 0.5f;
        float s = std::sin(halfAngle);
        Vector3 normalizedAxis = axis.Normalized();
        return Quaternion(
            normalizedAxis.x * s,
            normalizedAxis.y * s,
            normalizedAxis.z * s,
            std::cos(halfAngle)
        );
    }

    static float Dot(const Quaternion& a, const Quaternion& b) {
        return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
    }

    static Quaternion Slerp(const Quaternion& a, const Quaternion& b, float t) {
        float dot = Dot(a, b);

        // If negative dot, negate one quaternion to take shorter path
        Quaternion b2 = b;
        if (dot < 0.0f) {
            b2 = Quaternion(-b.x, -b.y, -b.z, -b.w);
            dot = -dot;
        }

        if (dot > 0.9995f) {
            // Linear interpolation for very close quaternions
            return Quaternion(
                a.x + t * (b2.x - a.x),
                a.y + t * (b2.y - a.y),
                a.z + t * (b2.z - a.z),
                a.w + t * (b2.w - a.w)
            ).Normalized();
        }

        float theta = std::acos(dot);
        float sinTheta = std::sin(theta);
        float wa = std::sin((1.0f - t) * theta) / sinTheta;
        float wb = std::sin(t * theta) / sinTheta;

        return Quaternion(
            a.x * wa + b2.x * wb,
            a.y * wa + b2.y * wb,
            a.z * wa + b2.z * wb,
            a.w * wa + b2.w * wb
        );
    }
};

} // namespace BasicTT
