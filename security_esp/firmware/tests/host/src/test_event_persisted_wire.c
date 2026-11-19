#include "unity.h"
#include "event_persisted_wire.h"
#include <stdio.h>
#include <stdlib.h>

void setUp(void) {}
void tearDown(void) {}

static char *read_fixture(const char *relative_path)
{
    char full_path[512];
    snprintf(full_path, sizeof(full_path), "../../../../docs/contracts/mqtt/v1/samples/%s", relative_path);
    FILE *f = fopen(full_path, "rb");
    TEST_ASSERT_NOT_NULL_MESSAGE(f, full_path);
    fseek(f, 0, SEEK_END);
    long n = ftell(f);
    fseek(f, 0, SEEK_SET);
    char *buf = malloc((size_t)n + 1);
    TEST_ASSERT_NOT_NULL(buf);
    fread(buf, 1, (size_t)n, f);
    buf[n] = '\0';
    fclose(f);
    return buf;
}

void test_parse_accept_with_phase(void)
{
    char *json = read_fixture("accept/event-persisted.json");
    event_persisted_wire_t out;
    char err[64];
    TEST_ASSERT_TRUE(event_persisted_parse(json, &out, err, sizeof(err)));
    TEST_ASSERT_TRUE(out.stored);
    TEST_ASSERT_TRUE(out.has_alarm_phase);
    TEST_ASSERT_FALSE(out.alarm_phase_ended);
    free(json);
}

void test_parse_legacy_without_phase_means_active(void)
{
    char *json = read_fixture("accept/event-persisted--without-phase.json");
    event_persisted_wire_t out;
    char err[64];
    TEST_ASSERT_TRUE(event_persisted_parse(json, &out, err, sizeof(err)));
    TEST_ASSERT_FALSE(out.has_alarm_phase);
    TEST_ASSERT_FALSE(out.alarm_phase_ended);
    free(json);
}

void test_reject_stored_false(void)
{
    char *json = read_fixture("reject/event-persisted--stored-false.json");
    event_persisted_wire_t out;
    char err[64];
    TEST_ASSERT_FALSE(event_persisted_parse(json, &out, err, sizeof(err)));
    free(json);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_parse_accept_with_phase);
    RUN_TEST(test_parse_legacy_without_phase_means_active);
    RUN_TEST(test_reject_stored_false);
    return UNITY_END();
}
