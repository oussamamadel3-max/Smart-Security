#include "unity.h"
#include "remote_command.h"
#include <string.h>

static command_dedup_t dedup;

void setUp(void) { command_dedup_init(&dedup); }
void tearDown(void) {}

static execute_command_t base_command(void)
{
    execute_command_t cmd;
    memset(&cmd, 0, sizeof(cmd));
    memcpy(cmd.command_id, "11111111-1111-1111-1111-111111111111", 36);
    cmd.command_id[36] = '\0';
    cmd.command_sequence = 5;
    cmd.ownership_version = 2;
    cmd.type = REMOTE_COMMAND_TYPE_ARM;
    cmd.expires_at_ms = 10000;
    return cmd;
}

static remote_command_context_t base_context(void)
{
    remote_command_context_t ctx;
    ctx.last_applied_command_sequence = 4;
    ctx.current_ownership_version = 2;
    ctx.clock_trusted = true;
    ctx.now_ms = 5000;
    return ctx;
}

static void test_valid_command_is_accepted(void)
{
    execute_command_t cmd = base_command();
    remote_command_context_t ctx = base_context();
    TEST_ASSERT_EQUAL(REMOTE_COMMAND_VERIFY_ACCEPT, remote_command_verify(&cmd, &ctx, &dedup));
}

static void test_duplicate_command_id_is_not_reexecuted(void)
{
    execute_command_t cmd = base_command();
    remote_command_context_t ctx = base_context();
    TEST_ASSERT_EQUAL(REMOTE_COMMAND_VERIFY_ACCEPT, remote_command_verify(&cmd, &ctx, &dedup));
    /* Same commandId retransmitted (e.g. MQTT redelivery) — same sequence too. */
    TEST_ASSERT_EQUAL(REMOTE_COMMAND_VERIFY_DUPLICATE, remote_command_verify(&cmd, &ctx, &dedup));
}

/* "لا تنفيذ قبل مزامنة إصدار الملكية" — ownership check wins even over an
 * otherwise-fresh sequence number. */
static void test_ownership_mismatch_rejected_even_with_fresh_sequence(void)
{
    execute_command_t cmd = base_command();
    cmd.ownership_version = 999;
    remote_command_context_t ctx = base_context();
    TEST_ASSERT_EQUAL(REMOTE_COMMAND_VERIFY_REJECT_OWNERSHIP_MISMATCH, remote_command_verify(&cmd, &ctx, &dedup));
}

static void test_stale_sequence_rejected(void)
{
    execute_command_t cmd = base_command();
    cmd.command_sequence = 4; /* == last_applied, not newer */
    remote_command_context_t ctx = base_context();
    TEST_ASSERT_EQUAL(REMOTE_COMMAND_VERIFY_REJECT_STALE_SEQUENCE, remote_command_verify(&cmd, &ctx, &dedup));
}

/* esp-context.md 2026-11-16 decision: cannot verify expiresAt without a
 * trusted clock -> reject rather than guess, even if expires_at_ms would
 * otherwise look unexpired. */
static void test_untrusted_clock_rejects_regardless_of_expiry_value(void)
{
    execute_command_t cmd = base_command();
    cmd.expires_at_ms = 999999; /* far in the future */
    remote_command_context_t ctx = base_context();
    ctx.clock_trusted = false;
    TEST_ASSERT_EQUAL(REMOTE_COMMAND_VERIFY_REJECT_CLOCK_NOT_TRUSTED, remote_command_verify(&cmd, &ctx, &dedup));
}

static void test_expired_command_rejected(void)
{
    execute_command_t cmd = base_command();
    cmd.expires_at_ms = 1000;
    remote_command_context_t ctx = base_context();
    ctx.now_ms = 5000;
    TEST_ASSERT_EQUAL(REMOTE_COMMAND_VERIFY_REJECT_EXPIRED, remote_command_verify(&cmd, &ctx, &dedup));
}

int main(void)
{
    UNITY_BEGIN();
    RUN_TEST(test_valid_command_is_accepted);
    RUN_TEST(test_duplicate_command_id_is_not_reexecuted);
    RUN_TEST(test_ownership_mismatch_rejected_even_with_fresh_sequence);
    RUN_TEST(test_stale_sequence_rejected);
    RUN_TEST(test_untrusted_clock_rejects_regardless_of_expiry_value);
    RUN_TEST(test_expired_command_rejected);
    return UNITY_END();
}
