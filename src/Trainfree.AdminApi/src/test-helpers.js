// Deterministically simulates a check-then-act race against real D1 (per
// CLAUDE-baseline.md's no-mocking-D1 rule, all reads/writes here are real): wraps `db`
// so that the exact `sql` statement resolves normally, then runs `sideEffect` -- a real
// D1 write -- before the caller's next statement executes. Only the ordering is
// synthesized; nothing about D1's own behavior is faked.
export function runAfterQueryResolves(db, sql, sideEffect) {
    return {
        prepare(query) {
            const statement = db.prepare(query);
            if (query !== sql) {
                return statement;
            }

            return {
                bind(...args) {
                    const bound = statement.bind(...args);
                    return {
                        async first(...args) {
                            const result = await bound.first(...args);
                            await sideEffect();
                            return result;
                        },
                    };
                },
            };
        },
    };
}
