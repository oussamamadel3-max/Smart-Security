#include "uuid128.h"
#include <string.h>
#include <stdio.h>

uuid128_t uuid128_generate_v4(uuid128_random_source_fn_t source, void *source_ctx)
{
    uuid128_t u;
    memset(&u, 0, sizeof(u));
    source(source_ctx, u.bytes, sizeof(u.bytes));
    u.bytes[6] = (uint8_t)((u.bytes[6] & 0x0F) | 0x40); /* version 4 */
    u.bytes[8] = (uint8_t)((u.bytes[8] & 0x3F) | 0x80); /* RFC 4122 variant */
    return u;
}

bool uuid128_is_nil(const uuid128_t *u)
{
    for (size_t i = 0; i < sizeof(u->bytes); i++) {
        if (u->bytes[i] != 0) {
            return false;
        }
    }
    return true;
}

bool uuid128_equal(const uuid128_t *a, const uuid128_t *b)
{
    return memcmp(a->bytes, b->bytes, sizeof(a->bytes)) == 0;
}

void uuid128_to_string(const uuid128_t *u, char out[37])
{
    const uint8_t *b = u->bytes;
    snprintf(out, 37,
             "%02x%02x%02x%02x-%02x%02x-%02x%02x-%02x%02x-%02x%02x%02x%02x%02x%02x",
             b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7],
             b[8], b[9], b[10], b[11], b[12], b[13], b[14], b[15]);
}

static int hex_val(char c)
{
    if (c >= '0' && c <= '9') return c - '0';
    if (c >= 'a' && c <= 'f') return 10 + (c - 'a');
    if (c >= 'A' && c <= 'F') return 10 + (c - 'A');
    return -1;
}

bool uuid128_from_string(const char *text, uuid128_t *out)
{
    if (text == NULL || strlen(text) != 36) {
        return false;
    }
    static const int dash_positions[] = { 8, 13, 18, 23 };
    for (size_t i = 0; i < sizeof(dash_positions) / sizeof(dash_positions[0]); i++) {
        if (text[dash_positions[i]] != '-') {
            return false;
        }
    }

    uint8_t bytes[16];
    size_t byte_index = 0;
    for (int i = 0; i < 36; ) {
        if (text[i] == '-') {
            i++;
            continue;
        }
        int hi = hex_val(text[i]);
        int lo = hex_val(text[i + 1]);
        if (hi < 0 || lo < 0) {
            return false;
        }
        bytes[byte_index++] = (uint8_t)((hi << 4) | lo);
        i += 2;
    }
    if (byte_index != 16) {
        return false;
    }
    memcpy(out->bytes, bytes, 16);
    return true;
}
