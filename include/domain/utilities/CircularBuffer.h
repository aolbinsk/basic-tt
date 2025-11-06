#pragma once

#include <vector>
#include <stdexcept>

namespace BasicTT {

template<typename T>
class CircularBuffer {
public:
    explicit CircularBuffer(size_t capacity)
        : m_capacity(capacity), m_size(0), m_head(0), m_tail(0) {
        m_buffer.resize(capacity);
    }

    void Push(const T& item) {
        m_buffer[m_tail] = item;
        m_tail = (m_tail + 1) % m_capacity;

        if (m_size < m_capacity) {
            m_size++;
        } else {
            m_head = (m_head + 1) % m_capacity;
        }
    }

    T Pop() {
        if (IsEmpty()) {
            throw std::runtime_error("CircularBuffer: Pop from empty buffer");
        }

        T item = m_buffer[m_head];
        m_head = (m_head + 1) % m_capacity;
        m_size--;
        return item;
    }

    const T& Get(size_t index) const {
        if (index >= m_size) {
            throw std::out_of_range("CircularBuffer: Index out of range");
        }

        size_t actualIndex = (m_head + index) % m_capacity;
        return m_buffer[actualIndex];
    }

    const T& GetNewest() const {
        if (IsEmpty()) {
            throw std::runtime_error("CircularBuffer: GetNewest from empty buffer");
        }

        size_t index = (m_tail == 0) ? (m_capacity - 1) : (m_tail - 1);
        return m_buffer[index];
    }

    const T& GetOldest() const {
        if (IsEmpty()) {
            throw std::runtime_error("CircularBuffer: GetOldest from empty buffer");
        }

        return m_buffer[m_head];
    }

    void Clear() {
        m_size = 0;
        m_head = 0;
        m_tail = 0;
    }

    bool IsEmpty() const { return m_size == 0; }
    bool IsFull() const { return m_size == m_capacity; }
    size_t Size() const { return m_size; }
    size_t Capacity() const { return m_capacity; }

private:
    std::vector<T> m_buffer;
    size_t m_capacity;
    size_t m_size;
    size_t m_head;
    size_t m_tail;
};

} // namespace BasicTT
