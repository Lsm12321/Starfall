"""Regenerate the title illustration through an already running local ComfyUI."""
import argparse
import json
from pathlib import Path
import time
import urllib.parse
import urllib.request


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--server', default='http://127.0.0.1:8188')
    parser.add_argument('--checkpoint', default='animagine-xl-4.0.safetensors')
    parser.add_argument('--seed', type=int, default=202610209)
    parser.add_argument('--output', type=Path, default=Path(__file__).resolve().parents[1] / 'Resources' / 'Art' / 'arena-generated.png')
    args = parser.parse_args()
    graph = json.loads(Path(__file__).with_name('arena_api.json').read_text(encoding='utf-8'))
    graph['1']['inputs']['ckpt_name'] = args.checkpoint
    graph['5']['inputs']['seed'] = args.seed
    payload = json.dumps({'prompt': graph, 'client_id': 'starfall-art'}).encode('utf-8')
    request = urllib.request.Request(args.server + '/prompt', data=payload, headers={'Content-Type': 'application/json'})
    response = json.load(urllib.request.urlopen(request, timeout=30))
    prompt_id = response['prompt_id']
    print('Queued:', prompt_id, flush=True)
    deadline = time.monotonic() + 600
    while time.monotonic() < deadline:
        history = json.load(urllib.request.urlopen(args.server + '/history/' + prompt_id, timeout=15))
        if prompt_id in history:
            item = history[prompt_id]
            if item['status']['status_str'] == 'error':
                raise RuntimeError(json.dumps(item['status'], ensure_ascii=False))
            images = item.get('outputs', {}).get('7', {}).get('images', [])
            if images:
                query = urllib.parse.urlencode(images[0])
                args.output.parent.mkdir(parents=True, exist_ok=True)
                with urllib.request.urlopen(args.server + '/view?' + query, timeout=30) as source:
                    args.output.write_bytes(source.read())
                print('Saved:', args.output)
                return
        time.sleep(2)
    raise TimeoutError('ComfyUI did not finish within ten minutes. Inspect its queue before retrying.')


if __name__ == '__main__':
    main()
