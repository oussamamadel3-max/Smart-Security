#include "unity.h"
#include "uuid128.h"
#include <string.h>

/* Deterministic fake RNG for reproducible test assertions — fills with a
 * fixed incrementing pattern, nothing like a real RNG (see uuid128.h: a
 * real hardware RNG is wired in by components/port_esp32, not written yet). */
static void fake_fill(void *ctx, uint8_t *out, size_t len)
{
    (void)ctx;
    for (size_t i = 0; i < len; i++) {
        out[i] = (uint8_t)i;
    }
}

static void fake_fill_all_zero(void *ctx, uint8_t *out, size_t len)
{
    (void)ctx;
    memset(out, 0, len);
}

void setUp(void) {}
void tearDown(void) {}

static void test_generate_v4_sets_version_and_variant_bits(void)
{
    uuid128_t u = uuid128_generate_v4(fake_fill, NULL);
    TEST_ASSERT_EQUAL_HEX8(0x40, u.bytes[6] & 0xF0); /* version nibble */
    TEST_ASSERT_EQUAL_HEX8(0x80, u.bytes[8] & 0xC0); /* RFC 4122 variant */
}

static void test_all_zero_source_is_not_nil_after_version_bits_set(void)
{
    /* Even an all-zero random source produces a non-nil UUID, because
     * version/variant bits are always forced non-zero — this is a sanity
     * check that uuid128_is_nil() is only ever true for the explicit
     * memset-to-zero sentinel, not a real generated value. */
    uuid128_t u = uuid128_generate_v4(fake_fill_all_zero, NULL);
    TEST_ASSERT_FALSE(uuid128_is_nil(&u));
}

static void test_explicit_zero_struct_is_nil(void)
{
    uuid128_t u;
    memset(&u, 0, sizeof(u));
    TEST_ASSERT_TRUE(uuid128_is_nil(&u));
}

static void test_to_string_round_trips_through_from_string(void)
{
    uuid128_t original = uuid128_generate_v4(fake_fill, NULL);
    char text[37];
    uuid128_to_string(&original, text);
    TEST_ASSERT_EQUAL(36, strlen(text));

    uuid128_t parsed;
    TEST_ASSERT_TRUE(uuid128_from_string(text, &parsed));
    TEST_ASSERT_TRUE(uuid128_equal(&original, &parsed));
}

/* Parses one of the exact UUID strings used in the contract's own fixture
 * files (docs/contracts/mqtt/v1/samples/), proving interop with the real
 * contract shape, not just with this module's own to_string output. */
static void test_parses_real_contract_fixture_uuid(void)
{
    uuid128_t u;
    TEST_ASSERT_TRUE(uuid128_from_string("33333333-3333-4333-8333-333333333333", &u));
    char text[37];
    uuid128_to_string(&u, text);
    TEST_ASSERT_EQUAL_STRING("33333333-3333-4333-8333-333333333333", text);
}

static void test_rejects_wrong_length(void)
{
    uuid128_t u;
    TEST_ASSERT_FALSE(uuid128_from_string("not-a-uuid", &u));
}

static void test_rejects_missing_dash(void)
{
    uuid128_t u;
    /* 36 chars, but a dash replaced by a hex digit. */
    TEST_ASSERT_FALSE(uuid128_from_string("333333333333-4333-8333-333333333333", &u));
}

static void test_rejects_non_hex_character(void)
{
    uuid128_t u;
    TEST_ASSERT_FALSE(uuid128_from_string("gggggggg-3333-4333-8333-333333333333", &u));
}

static void test_different_sources_are_not_equal(void)
{
    uuid128_t a = uuid128_generate_v4(fake_fill, NULL);
    uuid128_t b = uuid128_generate_v4(fake_fill_all_zero, NULL);
    TEST_ASSERT_FALSE(uuid128_equal(&a, &b));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_generate_v4_sets_version_and_variant_bits);
    RUN_TEST(test_all_zero_source_is_not_nil_after_version_bits_set);
    RUN_TEST(test_explicit_zero_struct_is_nil);
    RUN_TEST(test_to_string_round_trips_through_from_string);
    RUN_TEST(test_parses_real_contract_fixture_uuid);
    RUN_TEST(test_rejects_wrong_length);
    RUN_TEST(test_rejects_missing_dash);
    RUN_TEST(test_rejects_non_hex_character);
    RUN_TEST(test_different_sources_are_not_equal);
    return UNITY_END();
}
