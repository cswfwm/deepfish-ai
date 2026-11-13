#!/usr/bin/env python3
"""生成大肥鱼贴图（40x32，4 帧横排 -> 160x32）。

参考 pixnote.net 的 CC0 16x16 金鱼轮廓（公共领域），
这里重画为更胖更圆的原创造型并加眨眼/摆尾动画。
"""
import os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'Assets')

FRAME_W, FRAME_H, FRAMES = 40, 32, 4

BODY = (255, 137, 74, 255)
BODY_D = (206, 92, 44, 255)
BODY_L = (255, 189, 148, 255)
FIN = (255, 168, 116, 255)
EYE = (38, 30, 24, 255)
WHITE = (255, 255, 255, 255)
BLUSH = (255, 120, 120, 90)


def new_frame():
    return Image.new('RGBA', (FRAME_W, FRAME_H), (0, 0, 0, 0))


def outline(img):
    w, h = img.size
    src = img.load()
    out = img.copy()
    dst = out.load()
    for y in range(h):
        for x in range(w):
            if src[x, y][3] != 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and src[nx, ny][3] > 128:
                    dst[x, y] = BODY_D
                    break
    return out


def draw_fish(tail_up=False, blink=False):
    img = new_frame()
    d = img.__class__  # noqa: F841  (保持简单，直接用 ImageDraw)
    from PIL import ImageDraw
    d = ImageDraw.Draw(img)

    # 身体：一个大椭圆，占了画面大部分，圆滚滚
    d.ellipse([4, 8, 28, 26], fill=BODY)
    # 腹部高光
    d.ellipse([8, 17, 24, 25], fill=BODY_L)
    # 头顶深色
    d.arc([6, 9, 26, 22], 200, 340, fill=BODY_D)

    # 尾巴：两片扇形，随帧上下摆
    if tail_up:
        d.polygon([(27, 14), (37, 6), (36, 17)], fill=FIN)
        d.polygon([(27, 20), (37, 18), (35, 27)], fill=FIN)
    else:
        d.polygon([(27, 15), (37, 9), (35, 18)], fill=FIN)
        d.polygon([(27, 19), (37, 22), (35, 28)], fill=FIN)

    # 背鳍与胸鳍
    d.polygon([(14, 8), (20, 4), (23, 9)], fill=FIN)
    d.ellipse([12, 20, 17, 25], fill=FIN)

    # 大眼睛
    if blink:
        d.line([(10, 15), (16, 15)], fill=EYE, width=2)
    else:
        d.ellipse([9, 12, 16, 19], fill=WHITE)
        d.ellipse([11, 14, 15, 18], fill=EYE)
        d.point((12, 15), fill=WHITE)
    # 第二只眼（稍小，做出朝向感）
    if blink:
        d.line([(19, 15), (23, 15)], fill=EYE, width=2)
    else:
        d.ellipse([18, 14, 23, 19], fill=WHITE)
        d.ellipse([19, 15, 22, 18], fill=EYE)

    # 腮红
    d.ellipse([8, 20, 13, 23], fill=BLUSH)
    d.ellipse([19, 20, 24, 23], fill=BLUSH)

    # 小嘴
    d.arc([13, 20, 19, 25], 200, 340, fill=BODY_D)

    return outline(img)


def main():
    os.makedirs(OUT, exist_ok=True)
    sheet = Image.new('RGBA', (FRAME_W * FRAMES, FRAME_H), (0, 0, 0, 0))
    frames = [
        draw_fish(tail_up=False, blink=False),
        draw_fish(tail_up=True, blink=False),
        draw_fish(tail_up=False, blink=False),
        draw_fish(tail_up=True, blink=True),
    ]
    for i, frame in enumerate(frames):
        sheet.alpha_composite(frame, (i * FRAME_W, 0))

    path = os.path.join(OUT, 'BigFatFish.png')
    sheet.save(path)
    print('wrote', os.path.relpath(path, ROOT), sheet.size)

    # 物品图标：取第一帧缩小
    icon = frames[0].resize((24, 24), Image.LANCZOS)
    icon_path = os.path.join(OUT, 'BigFatFishItem.png')
    icon.save(icon_path)
    print('wrote', os.path.relpath(icon_path, ROOT), icon.size)


if __name__ == '__main__':
    main()
