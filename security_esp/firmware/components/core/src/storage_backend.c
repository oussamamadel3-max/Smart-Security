#include "storage_backend.h"

uint32_t storage_checksum(const void *data, size_t size)
{
    const uint8_t *bytes = (const uint8_t *)data;
    uint32_t sum = 0x811C9DC5u; /* FNV-1a offset basis */
    for (size_t i = 0; i < size; i++) {
        sum ^= bytes[i];
        sum *= 16777619u; /* FNV-1a prime */
    }
    return sum;
}
