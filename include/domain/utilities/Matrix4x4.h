#pragma once

#include "Vector3.h"
#include "Quaternion.h"
#include <cstring>

namespace BasicTT {

struct Matrix4x4 {
    float m[16]; // Column-major order

    Matrix4x4() {
        std::memset(m, 0, sizeof(m));
        m[0] = m[5] = m[10] = m[15] = 1.0f; // Identity
    }

    Matrix4x4(const float* data) {
        std::memcpy(m, data, sizeof(m));
    }

    // Matrix multiplication
    Matrix4x4 operator*(const Matrix4x4& other) const {
        Matrix4x4 result;
        for (int i = 0; i < 4; i++) {
            for (int j = 0; j < 4; j++) {
                result.m[i + j * 4] = 0;
                for (int k = 0; k < 4; k++) {
                    result.m[i + j * 4] += m[i + k * 4] * other.m[k + j * 4];
                }
            }
        }
        return result;
    }

    // Transform vector (position)
    Vector3 MultiplyPoint(const Vector3& v) const {
        float w = m[3] * v.x + m[7] * v.y + m[11] * v.z + m[15];
        if (w != 0.0f) w = 1.0f / w;
        return Vector3(
            (m[0] * v.x + m[4] * v.y + m[8] * v.z + m[12]) * w,
            (m[1] * v.x + m[5] * v.y + m[9] * v.z + m[13]) * w,
            (m[2] * v.x + m[6] * v.y + m[10] * v.z + m[14]) * w
        );
    }

    // Transform vector (direction)
    Vector3 MultiplyVector(const Vector3& v) const {
        return Vector3(
            m[0] * v.x + m[4] * v.y + m[8] * v.z,
            m[1] * v.x + m[5] * v.y + m[9] * v.z,
            m[2] * v.x + m[6] * v.y + m[10] * v.z
        );
    }

    // Static factory methods
    static Matrix4x4 Identity() {
        return Matrix4x4();
    }

    static Matrix4x4 Translation(const Vector3& v) {
        Matrix4x4 result;
        result.m[12] = v.x;
        result.m[13] = v.y;
        result.m[14] = v.z;
        return result;
    }

    static Matrix4x4 Rotation(const Quaternion& q) {
        Matrix4x4 result;

        float xx = q.x * q.x;
        float yy = q.y * q.y;
        float zz = q.z * q.z;
        float xy = q.x * q.y;
        float xz = q.x * q.z;
        float yz = q.y * q.z;
        float wx = q.w * q.x;
        float wy = q.w * q.y;
        float wz = q.w * q.z;

        result.m[0] = 1.0f - 2.0f * (yy + zz);
        result.m[1] = 2.0f * (xy + wz);
        result.m[2] = 2.0f * (xz - wy);
        result.m[3] = 0.0f;

        result.m[4] = 2.0f * (xy - wz);
        result.m[5] = 1.0f - 2.0f * (xx + zz);
        result.m[6] = 2.0f * (yz + wx);
        result.m[7] = 0.0f;

        result.m[8] = 2.0f * (xz + wy);
        result.m[9] = 2.0f * (yz - wx);
        result.m[10] = 1.0f - 2.0f * (xx + yy);
        result.m[11] = 0.0f;

        result.m[12] = 0.0f;
        result.m[13] = 0.0f;
        result.m[14] = 0.0f;
        result.m[15] = 1.0f;

        return result;
    }

    static Matrix4x4 Scale(const Vector3& v) {
        Matrix4x4 result;
        result.m[0] = v.x;
        result.m[5] = v.y;
        result.m[10] = v.z;
        return result;
    }

    static Matrix4x4 TRS(const Vector3& translation, const Quaternion& rotation, const Vector3& scale) {
        Matrix4x4 t = Translation(translation);
        Matrix4x4 r = Rotation(rotation);
        Matrix4x4 s = Scale(scale);
        return t * r * s;
    }

    static Matrix4x4 Perspective(float fovY, float aspect, float nearZ, float farZ) {
        Matrix4x4 result;
        std::memset(result.m, 0, sizeof(result.m));

        float tanHalfFovy = std::tan(fovY / 2.0f);

        result.m[0] = 1.0f / (aspect * tanHalfFovy);
        result.m[5] = 1.0f / tanHalfFovy;
        result.m[10] = -(farZ + nearZ) / (farZ - nearZ);
        result.m[11] = -1.0f;
        result.m[14] = -(2.0f * farZ * nearZ) / (farZ - nearZ);

        return result;
    }

    static Matrix4x4 LookAt(const Vector3& eye, const Vector3& center, const Vector3& up) {
        Vector3 f = (center - eye).Normalized();
        Vector3 s = Vector3::Cross(f, up).Normalized();
        Vector3 u = Vector3::Cross(s, f);

        Matrix4x4 result;
        result.m[0] = s.x;
        result.m[4] = s.y;
        result.m[8] = s.z;
        result.m[1] = u.x;
        result.m[5] = u.y;
        result.m[9] = u.z;
        result.m[2] = -f.x;
        result.m[6] = -f.y;
        result.m[10] = -f.z;
        result.m[12] = -Vector3::Dot(s, eye);
        result.m[13] = -Vector3::Dot(u, eye);
        result.m[14] = Vector3::Dot(f, eye);
        result.m[15] = 1.0f;

        return result;
    }

    Matrix4x4 Inverse() const {
        Matrix4x4 inv;
        float* invOut = inv.m;
        const float* m_ptr = m;

        invOut[0] = m_ptr[5] * m_ptr[10] * m_ptr[15] - m_ptr[5] * m_ptr[11] * m_ptr[14] -
                    m_ptr[9] * m_ptr[6] * m_ptr[15] + m_ptr[9] * m_ptr[7] * m_ptr[14] +
                    m_ptr[13] * m_ptr[6] * m_ptr[11] - m_ptr[13] * m_ptr[7] * m_ptr[10];

        invOut[4] = -m_ptr[4] * m_ptr[10] * m_ptr[15] + m_ptr[4] * m_ptr[11] * m_ptr[14] +
                     m_ptr[8] * m_ptr[6] * m_ptr[15] - m_ptr[8] * m_ptr[7] * m_ptr[14] -
                     m_ptr[12] * m_ptr[6] * m_ptr[11] + m_ptr[12] * m_ptr[7] * m_ptr[10];

        invOut[8] = m_ptr[4] * m_ptr[9] * m_ptr[15] - m_ptr[4] * m_ptr[11] * m_ptr[13] -
                    m_ptr[8] * m_ptr[5] * m_ptr[15] + m_ptr[8] * m_ptr[7] * m_ptr[13] +
                    m_ptr[12] * m_ptr[5] * m_ptr[11] - m_ptr[12] * m_ptr[7] * m_ptr[9];

        invOut[12] = -m_ptr[4] * m_ptr[9] * m_ptr[14] + m_ptr[4] * m_ptr[10] * m_ptr[13] +
                      m_ptr[8] * m_ptr[5] * m_ptr[14] - m_ptr[8] * m_ptr[6] * m_ptr[13] -
                      m_ptr[12] * m_ptr[5] * m_ptr[10] + m_ptr[12] * m_ptr[6] * m_ptr[9];

        invOut[1] = -m_ptr[1] * m_ptr[10] * m_ptr[15] + m_ptr[1] * m_ptr[11] * m_ptr[14] +
                     m_ptr[9] * m_ptr[2] * m_ptr[15] - m_ptr[9] * m_ptr[3] * m_ptr[14] -
                     m_ptr[13] * m_ptr[2] * m_ptr[11] + m_ptr[13] * m_ptr[3] * m_ptr[10];

        invOut[5] = m_ptr[0] * m_ptr[10] * m_ptr[15] - m_ptr[0] * m_ptr[11] * m_ptr[14] -
                    m_ptr[8] * m_ptr[2] * m_ptr[15] + m_ptr[8] * m_ptr[3] * m_ptr[14] +
                    m_ptr[12] * m_ptr[2] * m_ptr[11] - m_ptr[12] * m_ptr[3] * m_ptr[10];

        invOut[9] = -m_ptr[0] * m_ptr[9] * m_ptr[15] + m_ptr[0] * m_ptr[11] * m_ptr[13] +
                     m_ptr[8] * m_ptr[1] * m_ptr[15] - m_ptr[8] * m_ptr[3] * m_ptr[13] -
                     m_ptr[12] * m_ptr[1] * m_ptr[11] + m_ptr[12] * m_ptr[3] * m_ptr[9];

        invOut[13] = m_ptr[0] * m_ptr[9] * m_ptr[14] - m_ptr[0] * m_ptr[10] * m_ptr[13] -
                     m_ptr[8] * m_ptr[1] * m_ptr[14] + m_ptr[8] * m_ptr[2] * m_ptr[13] +
                     m_ptr[12] * m_ptr[1] * m_ptr[10] - m_ptr[12] * m_ptr[2] * m_ptr[9];

        invOut[2] = m_ptr[1] * m_ptr[6] * m_ptr[15] - m_ptr[1] * m_ptr[7] * m_ptr[14] -
                    m_ptr[5] * m_ptr[2] * m_ptr[15] + m_ptr[5] * m_ptr[3] * m_ptr[14] +
                    m_ptr[13] * m_ptr[2] * m_ptr[7] - m_ptr[13] * m_ptr[3] * m_ptr[6];

        invOut[6] = -m_ptr[0] * m_ptr[6] * m_ptr[15] + m_ptr[0] * m_ptr[7] * m_ptr[14] +
                     m_ptr[4] * m_ptr[2] * m_ptr[15] - m_ptr[4] * m_ptr[3] * m_ptr[14] -
                     m_ptr[12] * m_ptr[2] * m_ptr[7] + m_ptr[12] * m_ptr[3] * m_ptr[6];

        invOut[10] = m_ptr[0] * m_ptr[5] * m_ptr[15] - m_ptr[0] * m_ptr[7] * m_ptr[13] -
                     m_ptr[4] * m_ptr[1] * m_ptr[15] + m_ptr[4] * m_ptr[3] * m_ptr[13] +
                     m_ptr[12] * m_ptr[1] * m_ptr[7] - m_ptr[12] * m_ptr[3] * m_ptr[5];

        invOut[14] = -m_ptr[0] * m_ptr[5] * m_ptr[14] + m_ptr[0] * m_ptr[6] * m_ptr[13] +
                      m_ptr[4] * m_ptr[1] * m_ptr[14] - m_ptr[4] * m_ptr[2] * m_ptr[13] -
                      m_ptr[12] * m_ptr[1] * m_ptr[6] + m_ptr[12] * m_ptr[2] * m_ptr[5];

        invOut[3] = -m_ptr[1] * m_ptr[6] * m_ptr[11] + m_ptr[1] * m_ptr[7] * m_ptr[10] +
                     m_ptr[5] * m_ptr[2] * m_ptr[11] - m_ptr[5] * m_ptr[3] * m_ptr[10] -
                     m_ptr[9] * m_ptr[2] * m_ptr[7] + m_ptr[9] * m_ptr[3] * m_ptr[6];

        invOut[7] = m_ptr[0] * m_ptr[6] * m_ptr[11] - m_ptr[0] * m_ptr[7] * m_ptr[10] -
                    m_ptr[4] * m_ptr[2] * m_ptr[11] + m_ptr[4] * m_ptr[3] * m_ptr[10] +
                    m_ptr[8] * m_ptr[2] * m_ptr[7] - m_ptr[8] * m_ptr[3] * m_ptr[6];

        invOut[11] = -m_ptr[0] * m_ptr[5] * m_ptr[11] + m_ptr[0] * m_ptr[7] * m_ptr[9] +
                      m_ptr[4] * m_ptr[1] * m_ptr[11] - m_ptr[4] * m_ptr[3] * m_ptr[9] -
                      m_ptr[8] * m_ptr[1] * m_ptr[7] + m_ptr[8] * m_ptr[3] * m_ptr[5];

        invOut[15] = m_ptr[0] * m_ptr[5] * m_ptr[10] - m_ptr[0] * m_ptr[6] * m_ptr[9] -
                     m_ptr[4] * m_ptr[1] * m_ptr[10] + m_ptr[4] * m_ptr[2] * m_ptr[9] +
                     m_ptr[8] * m_ptr[1] * m_ptr[6] - m_ptr[8] * m_ptr[2] * m_ptr[5];

        float det = m_ptr[0] * invOut[0] + m_ptr[1] * invOut[4] + m_ptr[2] * invOut[8] + m_ptr[3] * invOut[12];

        if (det == 0.0f) {
            return Identity();
        }

        det = 1.0f / det;
        for (int i = 0; i < 16; i++) {
            invOut[i] *= det;
        }

        return inv;
    }
};

} // namespace BasicTT
