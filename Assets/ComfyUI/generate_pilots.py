"""Generate the five Starfall pilot illustrations from a local ComfyUI server."""
import argparse
import json
import time
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PROMPTS = [
    ("portrait_rei", 202610201, "masterpiece, best quality, very aesthetic, anime game key visual, solo young adult man, handsome Japanese sci-fi starship navigator, short tousled dark navy hair, luminous cyan eyes, white and teal high-collar flight jacket with fine luminous circuitry, calm observant expression, holding a compact elegant cyan pulse pistol, holographic star map, waist-up three-quarter pose, character centered, richly detailed face and costume, distant ruined orbital city at night, floating blue particles, polished premium anime illustration, cinematic rim light, deep navy and cyan palette, vertical portrait composition"),
    ("portrait_rikka", 202610202, "masterpiece, best quality, very aesthetic, anime game key visual, solo young adult woman, confident Japanese sci-fi gunner, vivid coral-red short hair with a long orange ribbon, bright amber eyes, asymmetrical black and vermilion armored flight jacket, tasteful practical full-coverage combat outfit, carrying a chunky futuristic twin-barrel scatter cannon with glowing orange vents, sparks and ember motes, waist-up three-quarter pose, cheeky fearless smile, character centered, orbital artillery platform at sunset, polished premium anime illustration, warm rim light, charcoal and vermilion palette, vertical portrait composition"),
    ("portrait_rin", 202610203, "masterpiece, best quality, very aesthetic, anime game key visual, solo young adult woman, elegant Japanese cryo-science officer, long pearl-silver hair with icy blue gradient, clear ice-blue eyes, white and cobalt insulated coat with crystalline armor details, holding a slender translucent frost lance with a luminous crystal head, snowflake hologram fragments, composed thoughtful expression, waist-up three-quarter pose, character centered, frozen research station among alien ruins, cool mist and tiny ice particles, polished premium anime illustration, blue-white palette, cinematic light, vertical portrait composition"),
    ("portrait_mei", 202610204, "masterpiece, best quality, very aesthetic, anime game key visual, solo young adult woman, cheerful Japanese shrine-mechanic pilot, soft lavender bob haircut with small twin buns and silver hair clips, violet eyes, white and plum tech-miko jacket over modest practical leggings, two sleek crescent orbit blades floating beside her hands, friendly steady expression, waist-up three-quarter pose, character centered, ancient alien shrine reimagined as a neon machine garden, glowing violet petals and turquoise runes, polished premium anime illustration, elegant luminous rim light, lavender and silver palette, vertical portrait composition"),
    ("portrait_yuna", 202610205, "masterpiece, best quality, very aesthetic, anime game key visual, solo young adult woman, cool Japanese storm courier and hacker, long midnight-violet ponytail with magenta streaks, vivid rose and cyan eyes, sleek dark indigo pilot bodysuit with magenta armor trim and a flowing short mantle, holding a compact electro-arc staff with branching blue lightning, poised determined expression, waist-up three-quarter pose, character centered, rain-soaked futuristic relay tower, neon signal rings and electric particles, polished premium anime illustration, dramatic violet-blue rim light, indigo magenta and cyan palette, vertical portrait composition"),
]
NEGATIVE = "low quality, worst quality, lowres, blurry, jpeg artifacts, text, logo, watermark, signature, subtitle, collage, multiple panels, multiple views, extra people, duplicate person, chibi, child, loli, nsfw, nudity, cleavage, bad anatomy, deformed hands, extra fingers, missing fingers, extra limbs, cropped face, out of frame"


def workflow(checkpoint, prompt, seed, width, height, steps):
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": checkpoint}},
        "8": {"class_type": "CLIPSetLastLayer", "inputs": {"clip": ["1", 1], "stop_at_clip_layer": -2}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 0], "text": prompt}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["8", 0], "text": NEGATIVE}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": width, "height": height, "batch_size": 1}},
        "5": {"class_type": "KSampler", "inputs": {"seed": seed, "steps": steps, "cfg": 6.5, "sampler_name": "euler_ancestral", "scheduler": "normal", "denoise": 1, "model": ["1", 0], "positive": ["2", 0], "negative": ["3", 0], "latent_image": ["4", 0]}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"filename_prefix": "starfall/pilots/" + str(seed), "images": ["6", 0]}},
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--server", default="http://127.0.0.1:8188")
    parser.add_argument("--checkpoint", default="animagine-xl-4.0.safetensors")
    parser.add_argument("--width", type=int, default=768)
    parser.add_argument("--height", type=int, default=1152)
    parser.add_argument("--steps", type=int, default=26)
    parser.add_argument("--output", type=Path, default=ROOT / "Assets" / "Resources" / "Art" / "Characters" / "Portraits")
    parser.add_argument("--limit", type=int, default=5)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    for key, seed, prompt in PROMPTS[:args.limit]:
        graph = workflow(args.checkpoint, prompt, seed, args.width, args.height, args.steps)
        request = urllib.request.Request(args.server + "/prompt", data=json.dumps({"prompt": graph, "client_id": "starfall-pilots"}).encode(), headers={"Content-Type": "application/json"})
        prompt_id = json.load(urllib.request.urlopen(request, timeout=30))["prompt_id"]
        print("Queued", key, prompt_id, flush=True)
        deadline = time.monotonic() + 600
        while time.monotonic() < deadline:
            history = json.load(urllib.request.urlopen(args.server + "/history/" + prompt_id, timeout=20))
            if prompt_id in history:
                item = history[prompt_id]
                if item["status"]["status_str"] == "error":
                    raise RuntimeError(json.dumps(item["status"], ensure_ascii=False))
                images = item.get("outputs", {}).get("7", {}).get("images", [])
                if images:
                    query = urllib.parse.urlencode(images[0])
                    target = args.output / (key + ".png")
                    with urllib.request.urlopen(args.server + "/view?" + query, timeout=60) as source:
                        target.write_bytes(source.read())
                    print("Saved", target, flush=True)
                    break
            time.sleep(2)
        else:
            raise TimeoutError("ComfyUI did not finish within ten minutes: " + key)

    (ROOT / "Assets" / "ComfyUI" / "pilot_prompts.json").write_text(
        json.dumps([{"asset": key, "seed": seed, "prompt": prompt, "negative": NEGATIVE} for key, seed, prompt in PROMPTS], ensure_ascii=False, indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
