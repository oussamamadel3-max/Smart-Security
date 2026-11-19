#include "unity.h"
#include "ownership_sync_wire.h"
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

void test_parse_accept_ownership_sync(void)
{
    char *json = read_fixture("accept/ownership-sync.json");
    ownership_sync_wire_t out;
    char err[64];
    TEST_ASSERT_TRUE(ownership_sync_parse(json, &out, err, sizeof(err)));
    TEST_ASSERT_EQUAL_UINT64(3, out.ownership_version);
    TEST_ASSERT_FALSE(out.is_linked);
    free(json);
}

void test_reject_missing_is_linked(void)
{
    char *json = read_fixture("reject/ownership-sync--missing-is-linked.json");
    ownership_sync_wire_t out;
    char err[64];
    TEST_ASSERT_FALSE(ownership_sync_parse(json, &out, err, sizeof(err)));
    free(json);
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_parse_accept_ownership_sync);
    RUN_TEST(test_reject_missing_is_linked);
    return UNITY_END();
}
