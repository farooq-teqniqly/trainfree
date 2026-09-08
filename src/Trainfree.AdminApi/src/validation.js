const MIN_NAME_LENGTH = 4;
const MAX_NAME_LENGTH = 100;

export function validateName(name) {
    if (typeof name !== "string") {
        return { valid: false, error: "name is required" };
    }

    const trimmed = name.trim();

    if (trimmed.length < MIN_NAME_LENGTH || trimmed.length > MAX_NAME_LENGTH) {
        return {
            valid: false,
            error: `name must be between ${MIN_NAME_LENGTH} and ${MAX_NAME_LENGTH} characters`,
        };
    }

    return { valid: true, name: trimmed };
}

export function validateProgramName(name) {
    return validateName(name);
}

export function validateSessionName(name) {
    return validateName(name);
}

export function validatePhaseName(name) {
    return validateName(name);
}

export function validateExerciseName(name) {
    return validateName(name);
}

const PROGRAM_EXERCISE_TYPES = ["Reps", "Timed"];
const PROGRAM_EXERCISE_SIDES = ["Both", "Left", "Right"];

function isPositiveInteger(value) {
    return typeof value === "number" && Number.isInteger(value) && value > 0;
}

function validateWeight(body) {
    if (body.weight === undefined) {
        return { valid: true, weight: 0 };
    }
    if (typeof body.weight !== "number" || body.weight < 0) {
        return { valid: false, error: "weight must be a non-negative number" };
    }
    return { valid: true, weight: body.weight };
}

function validateSide(body) {
    if (body.side === undefined) {
        return { valid: true, side: "Both" };
    }
    if (!PROGRAM_EXERCISE_SIDES.includes(body.side)) {
        return {
            valid: false,
            error: `side must be one of ${PROGRAM_EXERCISE_SIDES.join(", ")}`,
        };
    }
    return { valid: true, side: body.side };
}

export function validateCreateProgramExercise(body) {
    if (typeof body.exerciseId !== "string" || body.exerciseId.length === 0) {
        return { valid: false, error: "exerciseId is required" };
    }

    if (!PROGRAM_EXERCISE_TYPES.includes(body.type)) {
        return {
            valid: false,
            error: `type must be one of ${PROGRAM_EXERCISE_TYPES.join(", ")}`,
        };
    }

    const countField = body.type === "Reps" ? "reps" : "durationSeconds";
    const mismatchedField = body.type === "Reps" ? "durationSeconds" : "reps";
    if (body[mismatchedField] !== undefined) {
        return {
            valid: false,
            error: `${mismatchedField} is not allowed when type is ${body.type}`,
        };
    }

    if (!isPositiveInteger(body[countField])) {
        return { valid: false, error: `${countField} must be a positive integer` };
    }

    if (!isPositiveInteger(body.sets)) {
        return { valid: false, error: "sets must be a positive integer" };
    }

    if (!isPositiveInteger(body.restSeconds)) {
        return { valid: false, error: "restSeconds must be a positive integer" };
    }

    const weightResult = validateWeight(body);
    if (!weightResult.valid) {
        return weightResult;
    }

    const sideResult = validateSide(body);
    if (!sideResult.valid) {
        return sideResult;
    }

    return {
        valid: true,
        exerciseId: body.exerciseId,
        type: body.type,
        [countField]: body[countField],
        sets: body.sets,
        restSeconds: body.restSeconds,
        weight: weightResult.weight,
        side: sideResult.side,
    };
}

export function validateUpdateProgramExercise(body, existingType) {
    if ("exerciseId" in body || "type" in body) {
        return { valid: false, error: "exerciseId and type cannot be changed" };
    }

    const updates = {};

    if (body.reps !== undefined) {
        if (existingType === "Timed") {
            return { valid: false, error: "reps is not allowed when type is Timed" };
        }
        if (!isPositiveInteger(body.reps)) {
            return { valid: false, error: "reps must be a positive integer" };
        }
        updates.reps = body.reps;
    }

    if (body.durationSeconds !== undefined) {
        if (existingType === "Reps") {
            return { valid: false, error: "durationSeconds is not allowed when type is Reps" };
        }
        if (!isPositiveInteger(body.durationSeconds)) {
            return { valid: false, error: "durationSeconds must be a positive integer" };
        }
        updates.durationSeconds = body.durationSeconds;
    }

    if (body.sets !== undefined) {
        if (!isPositiveInteger(body.sets)) {
            return { valid: false, error: "sets must be a positive integer" };
        }
        updates.sets = body.sets;
    }

    if (body.restSeconds !== undefined) {
        if (!isPositiveInteger(body.restSeconds)) {
            return { valid: false, error: "restSeconds must be a positive integer" };
        }
        updates.restSeconds = body.restSeconds;
    }

    if (body.weight !== undefined) {
        if (typeof body.weight !== "number" || body.weight < 0) {
            return { valid: false, error: "weight must be a non-negative number" };
        }
        updates.weight = body.weight;
    }

    if (body.side !== undefined) {
        if (!PROGRAM_EXERCISE_SIDES.includes(body.side)) {
            return {
                valid: false,
                error: `side must be one of ${PROGRAM_EXERCISE_SIDES.join(", ")}`,
            };
        }
        updates.side = body.side;
    }

    if (Object.keys(updates).length === 0) {
        return { valid: false, error: "at least one field must be provided" };
    }

    return { valid: true, updates };
}
