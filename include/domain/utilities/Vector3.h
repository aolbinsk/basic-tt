#pragma once

#include <cmath>

namespace BasicTT {

struct Vector3 {
    float x, y, z;

    Vector3() : x(0.0f), y(0.0f), z(0.0f) {}
    Vector3(float x, float y, float z) : x(x), y(y), z(z) {}

    // Vector operations
    Vector3 operator+(const Vector3& other) const {
        return Vector3(x + other.x, y + other.y, z + other.z);
    }

    Vector3 operator-(const Vector3& other) const {
        return Vector3(x - other.x, y - other.y, z - other.z);
    }

    Vector3 operator*(float scalar) const {
        return Vector3(x * scalar, y * scalar, z * scalar);
    }

    Vector3 operator/(float scalar) const {
        return Vector3(x / scalar, y / scalar, z / scalar);
    }

    Vector3& operator+=(const Vector3& other) {
        x += other.x;
        y += other.y;
        z += other.z;
        return *this;
    }

    Vector3& operator-=(const Vector3& other) {
        x -= other.x;
        y -= other.y;
        z -= other.z;
        return *this;
    }

    Vector3& operator*=(float scalar) {
        x *= scalar;
        y *= scalar;
        z *= scalar;
        return *this;
    }

    Vector3 operator-() const {
        return Vector3(-x, -y, -z);
    }

    // Utility methods
    float Magnitude() const {
        return std::sqrt(x * x + y * y + z * z);
    }

    float SqrMagnitude() const {
        return x * x + y * y + z * z;
    }

    Vector3 Normalized() const {
        float mag = Magnitude();
        if (mag > 1e-6f) {
            return *this / mag;
        }
        return Vector3(0, 0, 0);
    }

    void Normalize() {
        float mag = Magnitude();
        if (mag > 1e-6f) {
            *this /= mag;
        }
    }

    // Static methods
    static float Dot(const Vector3& a, const Vector3& b) {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    static Vector3 Cross(const Vector3& a, const Vector3& b) {
        return Vector3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x
        );
    }

    static float Distance(const Vector3& a, const Vector3& b) {
        return (b - a).Magnitude();
    }

    static Vector3 Lerp(const Vector3& a, const Vector3& b, float t) {
        return a + (b - a) * t;
    }

    static Vector3 Zero() { return Vector3(0, 0, 0); }
    static Vector3 One() { return Vector3(1, 1, 1); }
    static Vector3 Up() { return Vector3(0, 1, 0); }
    static Vector3 Down() { return Vector3(0, -1, 0); }
    static Vector3 Left() { return Vector3(-1, 0, 0); }
    static Vector3 Right() { return Vector3(1, 0, 0); }
    static Vector3 Forward() { return Vector3(0, 0, 1); }
    static Vector3 Back() { return Vector3(0, 0, -1); }
};

} // namespace BasicTT
