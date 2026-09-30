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
// image is not needlessly re-encoded.
export async function resizeImage(bytes, contentType) {
  const source = await createImageBitmap(new Blob([bytes], { type: contentType }));
  try {
    const target = computeTargetSize(source.width, source.height);
    if (target.width === source.width && target.height === source.height) {
      return bytes;
    }
    const canvas = new OffscreenCanvas(target.width, target.height);
    canvas.getContext("2d").drawImage(source, 0, 0, target.width, target.height);
    const blob = await canvas.convertToBlob({ type: contentType, quality: 0.92 });
    return new Uint8Array(await blob.arrayBuffer());
  } finally {
    source.close();
  }
}
