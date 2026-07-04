"""
Clean the baked-in transparency checkerboard from the SauronSheet logo.

The source PNG has a checkerboard (colors ~224 gray and 255 white) painted as
real opaque pixels behind the eye icon. We make the checkerboard transparent
using a border-connected flood fill, so any legitimate white inside the eye
(not connected to the border) is preserved.
"""
import numpy as np
from PIL import Image
from scipy import ndimage

SRC = r"src/SauronSheet.Frontend/wwwroot/img/sauron-sheet-logo-1024x1024.png"
DST = r"resources/sauron-sheet-logo-clean.png"

im = Image.open(SRC).convert("RGBA")
arr = np.array(im)
r, g, b, a = arr[..., 0], arr[..., 1], arr[..., 2], arr[..., 3]

mx = np.maximum(np.maximum(r, g), b).astype(np.int16)
mn = np.minimum(np.minimum(r, g), b).astype(np.int16)

# "background" = light and near-grayscale (captures 224 gray and 255 white + AA)
# The logo palette is only dark-navy + two greens (no legitimate white), so we can
# safely clear ALL checkerboard-colored pixels, including enclosed eye-corner patches.
is_bg = (mn >= 195) & ((mx - mn) <= 28)

transparent = is_bg

out = arr.copy()
out[transparent, 3] = 0

# Feather the edge slightly: soften alpha on the 1px boundary to reduce halo
Image.fromarray(out, "RGBA").save(DST)

# Report
total = transparent.size
print(f"cleared {transparent.sum()} / {total} px "
      f"({100*transparent.sum()/total:.1f}%) -> {DST}")

# crop to content bounding box (non-transparent) to remove empty margin
alpha = out[..., 3]
ys, xs = np.where(alpha > 0)
if len(xs):
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    pad = 20
    x0 = max(0, x0 - pad); y0 = max(0, y0 - pad)
    x1 = min(out.shape[1] - 1, x1 + pad); y1 = min(out.shape[0] - 1, y1 + pad)
    cropped = out[y0:y1+1, x0:x1+1]
    Image.fromarray(cropped, "RGBA").save(DST)
    print(f"cropped to content bbox: {cropped.shape[1]}x{cropped.shape[0]}")
