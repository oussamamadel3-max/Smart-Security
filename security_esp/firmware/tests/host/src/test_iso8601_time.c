#include "unity.h"
#include "iso8601_time.h"

void setUp(void) {}
void tearDown(void) {}

static void test_epoch_zero(void)
{
    int64_t ms;
    TEST_ASSERT_TRUE(iso8601_utc_to_epoch_ms("1970-01-01T00:00:00Z", &ms));
    TEST_ASSERT_EQUAL_INT64(0, ms);
}

static void test_one_second_after_epoch(void)
{
    int64_t ms;
    TEST_ASSERT_TRUE(iso8601_utc_to_epoch_ms("1970-01-01T00:00:01Z", &ms));
    TEST_ASSERT_EQUAL_INT64(1000, ms);
}

/* Well-known reference constant: 2000-01-01T00:00:00Z = 946684800s. */
static void test_known_reference_year_2000(void)
{
    int64_t ms;
    TEST_ASSERT_TRUE(iso8601_utc_to_epoch_ms("2000-01-01T00:00:00Z", &ms));
    TEST_ASSERT_EQUAL_INT64(946684800000LL, ms);
}

/* Well-known reference constant: 2024-01-01T00:00:00Z = 1704067200s
 * (also exercises a leap-year boundary: 2024 is a leap year). */
static void test_known_reference_year_2024(void)
{
    int64_t ms;
    TEST_ASSERT_TRUE(iso8601_utc_to_epoch_ms("2024-01-01T00:00:00Z", &ms));
    TEST_ASSERT_EQUAL_INT64(1704067200000LL, ms);
}

static void test_rejects_wrong_length(void)
{
    int64_t ms;
    TEST_ASSERT_FALSE(iso8601_utc_to_epoch_ms("2026-10-25T12:00:00", &ms)); /* missing Z */
    TEST_ASSERT_FALSE(iso8601_utc_to_epoch_ms("2026-10-25T12:00:00.000Z", &ms)); /* fractional seconds, not emitted by the draft contract */
}

static void test_rejects_lowercase_z(void)
{
    int64_t ms;
    TEST_ASSERT_FALSE(iso8601_utc_to_epoch_ms("2026-10-25T12:00:00z", &ms));
}

static void test_rejects_out_of_range_month(void)
{
    int64_t ms;
    TEST_ASSERT_FALSE(iso8601_utc_to_epoch_ms("2026-13-25T12:00:00Z", &ms));
}

static void test_rejects_null(void)
{
    int64_t ms;
    TEST_ASSERT_FALSE(iso8601_utc_to_epoch_ms(NULL, &ms));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_epoch_zero);
    RUN_TEST(test_one_second_after_epoch);
    RUN_TEST(test_known_reference_year_2000);
    RUN_TEST(test_known_reference_year_2024);
    RUN_TEST(test_rejects_wrong_length);
    RUN_TEST(test_rejects_lowercase_z);
    RUN_TEST(test_rejects_out_of_range_month);
    RUN_TEST(test_rejects_null);
    return UNITY_END();
}
