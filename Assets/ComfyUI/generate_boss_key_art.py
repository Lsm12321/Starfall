"""Generate wide anime sci-fi key art for the three distinct raid bosses.

Requires a running local ComfyUI instance and Animagine XL 4.0. Images are saved
directly into Unity Resources so they are available in both editor and builds.
"""
import argparse
import json
import time
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Assets" / "Resources" / "Art"
PROMPTS_PATH = Path(__file__).with_name("boss_keyart_prompts.json")
NEGATIVE = (
    "low quality, worst quality, lowres, blurry, jpeg artifacts, text, logo, watermark, "
    "signature, subtitle, typography, collage, multiple panels, multiple views, split screen, "
    "extra people, duplicate boss, malformed anatomy, cropped boss, out of frame, oversaturated"
)

ART = [
    (
        "boss_keyart_warden", 731611,
        "masterpiece, best quality, anime sci-fi game boss key visual, widescreen 16:9 cinematic composition, "
        "an imposing colossal mechanical sentinel called the Dread Warden, dark blue-black ceramic armor, "
        "broad angular shoulders, antique gold armor edges, bright cyan contained star reactor in its chest, "
        "small cold cyan visor, two tall crown antennae, fragmented concentric halo, four articulated arms, "
        "standing on a ruined orbital shrine, turquoise runes, drifting debris and blue embers, "
        "boss occupies the right half with full silhouette visible, atmospheric negative space on the left, "
        "dramatic low angle, premium Japanese anime key art, precise linework, painterly light, "
        "deep navy, cyan and antique gold palette, no words"
    ),
    (
        "boss_keyart_herald", 731612,
        "masterpiece, best quality, anime sci-fi game boss key visual, widescreen 16:9 cinematic composition, "
        "a fast floating phase oracle called the Echo Herald, elegant ivory porcelain mask with narrow violet eyes, "
        "dark indigo layered mantle, antique gold floating shards, broken orbit rings and long luminous violet ribbons, "
        "appearing through a fractured space-time rift above an abandoned station, trails of magenta and cyan light, "
        "dynamic teleportation pose, boss occupies the right half with full silhouette visible, "
        "atmospheric negative space on the left, premium Japanese anime game key art, crisp details, "
        "dramatic rim light, violet, indigo and warm gold palette, no words"
    ),
    (
        "boss_keyart_star_eater", 731613,
        "masterpiece, best quality, anime sci-fi game boss key visual, widescreen 16:9 cinematic composition, "
        "a stationary colossal final boss called the Star Eater, a cathedral-sized monolithic basalt creature "
        "anchored to a circular alien dais, broad low silhouette, layered violet crystal armor, crown of long shards, "
        "enormous black event-horizon core surrounded by luminous amethyst rings, ground-bound gold seal runes, "
        "starless cosmic sanctum and distant fractured planets, powerful but readable silhouette, "
        "boss occupies the right half with whole body visible, atmospheric negative space on the left, "
        "premium Japanese anime game key art, meticulous painterly detail, high contrast violet and midnight palette, "
        "no words"
    ),
]


def workflow(checkpoint, prompt, seed, width, height, steps):
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": checkpoint}},
        "8": {"class_type": "CLIPSetLastLayer", "inputs": {"clip": ["1", 1], "stop_at_clip_layer": -2}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 0], "text": prompt}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 0], "text": NEGATIVE}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": width, "height": height, "batch_size": 1}},
        "5": {"class_type": "KSampler", "inputs": {
            "seed": seed, "steps": steps, "cfg": 6.5, "sampler_name": "euler_ancestral",
            "scheduler": "normal", "denoise": 1, "model": ["1", 0],
            "positive": ["2", 0], "negative": ["3", 0], "latent_image": ["4", 0]
        }},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"filename_prefix": "starfall/boss_keyart/" + str(seed), "images": ["6", 0]}},
    }


def fetch_json(url, timeout=30):
    with urllib.request.urlopen(url, timeout=timeout) as response:
        return json.load(response)


def generate_one(server, checkpoint, width, height, steps, asset, seed, prompt):
    graph = workflow(checkpoint, prompt, seed, width, height, steps)
    body = json.dumps({"prompt": graph, "client_id": "starfall-boss-keyart"}).encode("utf-8")
    request = urllib.request.Request(server + "/prompt", data=body,
                                     headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(request, timeout=30) as response:
        prompt_id = json.load(response)["prompt_id"]
    print("Queued", asset, prompt_id, flush=True)
    deadline = time.monotonic() + 900
    while time.monotonic() < deadline:
        history = fetch_json(server + "/history/" + prompt_id, timeout=25)
        if prompt_id in history:
            item = history[prompt_id]
            if item.get("status", {}).get("status_str") == "error":
                raise RuntimeError(json.dumps(item["status"], ensure_ascii=False))
            images = item.get("outputs", {}).get("7", {}).get("images", [])
            if images:
                query = urllib.parse.urlencode(images[0])
                target = OUTPUT / (asset + ".png")
                target.parent.mkdir(parents=True, exist_ok=True)
                with urllib.request.urlopen(server + "/view?" + query, timeout=60) as response:
                    target.write_bytes(response.read())
                print("Saved", target, flush=True)
                return
        time.sleep(2)
    raise TimeoutError("ComfyUI did not finish within 15 minutes: " + asset)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--server", default="http://127.0.0.1:8188")
    parser.add_argument("--checkpoint", default="animagine-xl-4.0.safetensors")
    parser.add_argument("--width", type=int, default=1024)
    parser.add_argument("--height", type=int, default=576)
    parser.add_argument("--steps", type=int, default=24)
    args = parser.parse_args()
    # Fail early if the local service is unavailable.
    fetch_json(args.server + "/system_stats", timeout=10)
    for asset, seed, prompt in ART:
        generate_one(args.server, args.checkpoint, args.width, args.height,
                     args.steps, asset, seed, prompt)
    PROMPTS_PATH.write_text(json.dumps([
        {"asset": name, "seed": seed, "prompt": prompt, "negative": NEGATIVE}
        for name, seed, prompt in ART
    ], ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
