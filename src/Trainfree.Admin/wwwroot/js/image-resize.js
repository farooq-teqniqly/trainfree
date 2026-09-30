export const MAX_EDGE = 800;

// Scales so the longer edge is at most maxEdge; never enlarges.
export function computeTargetSize(width, height, maxEdge = MAX_EDGE) {
  const longer = Math.max(width, height);
  if (longer <= maxEdge) {
    return { width, height };
  }
  const scale = maxEdge / longer;
  return {
    width: Math.max(1, Math.round(width * scale)),
    height: Math.max(1, Math.round(height * scale)),
  };
}

// Returns the original bytes untouched when no downscale is needed, so a small
// image is not needlessly re-encoded. When a downscale is needed the downscaled
// bytes are always returned, even if larger than the original, so the 800 px
// longer-edge limit holds. The `codec` seam lets tests stub the browser APIs.
export const browserCodec = {
  decode: (bytes, contentType) => createImageBitmap(new Blob([bytes], { type: contentType })),
  async encode(source, width, height, contentType) {
    const canvas = new OffscreenCanvas(width, height);
    canvas.getContext("2d").drawImage(source, 0, 0, width, height);
    const blob = await canvas.convertToBlob({ type: contentType, quality: 0.92 });
    return new Uint8Array(await blob.arrayBuffer());
  },
};

export async function resizeImage(bytes, contentType, codec = browserCodec) {
  const source = await codec.decode(bytes, contentType);
  try {
    const target = computeTargetSize(source.width, source.height);
    if (target.width === source.width && target.height === source.height) {
      return bytes;
    }
    return await codec.encode(source, target.width, target.height, contentType);
  } finally {
    source.close();
  }
}
