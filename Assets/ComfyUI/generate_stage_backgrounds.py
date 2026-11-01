"""Generate the three Starfall stage backgrounds through a local ComfyUI API."""
import argparse
import json
import time
import urllib.parse
import urllib.request
from pathlib import Path


PROMPTS = [
    (
        "stage1",
        "masterpiece, best quality, very aesthetic, environment concept art, no humans, no characters, no enemies, science fiction anime game background, a clean abandoned orbital research deck made of dark blue steel tiles, turquoise power conduits, small magenta crystal fragments, a calm circular landing pad at the exact center, wide top-down three-quarter view, clear readable floor, cinematic rim lighting, subtle cyan glow, restrained detail around the edges, deep navy atmosphere",
        202610251,
    ),
    (
        "stage2",
        "masterpiece, best quality, very aesthetic, environment concept art, no humans, no characters, no enemies, science fiction anime game background, the same abandoned orbital research deck now fractured by a violet dimensional rift, dark indigo steel floor tiles, luminous lavender rings and angular energy markers, a calm circular arena at the exact center, floating crystal debris kept near the perimeter, wide top-down three-quarter view, clear readable floor, cinematic purple and cyan lighting, escalating but playable atmosphere",
        202610252,
    ),
    (
        "stage3",
        "masterpiece, best quality, very aesthetic, environment concept art, no humans, no characters, no enemies, science fiction anime game background, the final stage of the same orbital arena transformed into a colossal star-core chamber, near-black navy floor tiles, hot violet and white energy rings, radiant pink cosmic fragments around the perimeter, a large stable circular platform at the exact center, wide top-down three-quarter view, clear readable floor, dramatic endgame lighting, intense but uncluttered composition, high contrast sci-fi anime environment",
        202610253,
    ),
]

NEGATIVE = "low quality, worst quality, blurry, text, watermark, logo, people, character, enemy, boss, weapon, foreground subject, close-up, collage, panels, split screen, sprite sheet, grid distortion, illegible floor, excessive fog, oversaturated orange"


def request_json(url, payload=None, timeout=30):
    if payload is None:
        with urllib.request.urlopen(url, timeout=timeout) as response:
            return json.load(response)
    body = json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(url, data=body, headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(request, timeout=timeout) as response:
        return json.load(response)


def generate(server, graph_template, key, positive, seed, output_dir):
    graph = json.loads(json.dumps(graph_template))
    graph["1"]["inputs"]["ckpt_name"] = "animagine-xl-4.0.safetensors"
    graph["2"]["inputs"]["text"] = positive
    graph["3"]["inputs"]["text"] = NEGATIVE
    graph["4"]["inputs"]["width"] = 1216
    graph["4"]["inputs"]["height"] = 832
    graph["5"]["inputs"]["seed"] = seed
    graph["7"]["inputs"]["filename_prefix"] = "Starfall/stage_background_" + key
    result = request_json(server + "/prompt", {"prompt": graph, "client_id": "starfall-stage-backgrounds"})
    prompt_id = result["prompt_id"]
    print("Queued", key, prompt_id, flush=True)
    deadline = time.monotonic() + 900
    while time.monotonic() < deadline:
        history = request_json(server + "/history/" + prompt_id, timeout=20)
        if prompt_id in history:
            item = history[prompt_id]
            if item.get("status", {}).get("status_str") == "error":
                raise RuntimeError(json.dumps(item["status"], ensure_ascii=False))
            images = item.get("outputs", {}).get("7", {}).get("images", [])
            if images:
                query = urllib.parse.urlencode(images[0])
                output_dir.mkdir(parents=True, exist_ok=True)
                target = output_dir / ("arena-floor-" + key + ".png")
                with urllib.request.urlopen(server + "/view?" + query, timeout=60) as source:
                    target.write_bytes(source.read())
                print("Saved", target, flush=True)
                return
        time.sleep(3)
    raise TimeoutError("ComfyUI did not finish " + key + " within fifteen minutes")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--server", default="http://127.0.0.1:8189")
    parser.add_argument("--workflow", type=Path, default=Path(__file__).with_name("arena_api.json"))
    parser.add_argument("--output-dir", type=Path, default=Path(__file__).resolve().parents[1] / "Resources" / "Art" / "Arena")
    args = parser.parse_args()
    graph = json.loads(args.workflow.read_text(encoding="utf-8"))
    for key, prompt, seed in PROMPTS:
        generate(args.server, graph, key, prompt, seed, args.output_dir)


if __name__ == "__main__":
    main()
