import test from "node:test";
import assert from "node:assert/strict";
import { computeTargetSize, resizeImage } from "../../src/Trainfree.Admin/wwwroot/js/image-resize.js";

test("computeTargetSize scales by longer edge and keeps aspect ratio", () => {
  const cases = [
    [1600, 3200, { width: 400, height: 800 }],
    [3200, 1600, { width: 800, height: 400 }],
    [1600, 1600, { width: 800, height: 800 }],
    [801, 1, { width: 800, height: 1 }],
    [1, 801, { width: 1, height: 800 }],
    [4000, 3000, { width: 800, height: 600 }],
  ];
  for (const [w, h, expected] of cases) {
    assert.deepEqual(computeTargetSize(w, h), expected, `${w}x${h}`);
  }
});

test("computeTargetSize leaves images at or under 800 unchanged", () => {
  for (const [w, h] of [[800, 800], [800, 10], [10, 800], [640, 480], [1, 1]]) {
    assert.deepEqual(computeTargetSize(w, h), { width: w, height: h }, `${w}x${h}`);
  }
});

function fakeCodec(width, height, encoded) {
  const state = { closed: false, encodeArgs: null };
  return {
    state,
    decode: async () => ({ width, height, close: () => { state.closed = true; } }),
    encode: async (_s, w, h) => { state.encodeArgs = [w, h]; return encoded; },
  };
}

test("resizeImage returns original bytes when no downscale is needed", async () => {
  const bytes = new Uint8Array([1, 2, 3]);
  const codec = fakeCodec(640, 480, new Uint8Array([9]));
  const result = await resizeImage(bytes, "image/png", codec);
  assert.equal(result, bytes);
  assert.equal(codec.state.encodeArgs, null);
  assert.ok(codec.state.closed);
});

test("resizeImage returns downscaled bytes even when larger than the original", async () => {
  const bytes = new Uint8Array(10);
  const larger = new Uint8Array(50);
  const codec = fakeCodec(1600, 3200, larger);
  const result = await resizeImage(bytes, "image/png", codec);
  assert.equal(result, larger);
  assert.deepEqual(codec.state.encodeArgs, [400, 800]);
  assert.ok(codec.state.closed);
});

test("resizeImage returns downscaled bytes when smaller than the original", async () => {
  const smaller = new Uint8Array(2);
  const result = await resizeImage(new Uint8Array(10), "image/jpeg", fakeCodec(2000, 1000, smaller));
  assert.equal(result, smaller);
});
