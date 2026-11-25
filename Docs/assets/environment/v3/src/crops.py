import sys
from PIL import Image
import numpy as np
import room, outside
from style import save
full = {}
full["room_d1"] = room.compose(day=1)
full["room_night"] = room.compose(day=5, night=True)
full["room_tornado"] = room.compose(day=4, disaster="tornado")
full["street_d1"] = outside.compose(day=1)
C = [("X01", "room_d1", (180, 30, 560, 230)), ("X02", "room_d1", (40, 210, 330, 330)), ("X03", "room_d1", (1560, 424, 1740, 664)),
     ("X04", "room_d1", (270, 96, 350, 210)), ("X05", "room_night", (1050, 96, 1370, 360)), ("X06", "street_d1", (1540, 130, 1760, 330)),
     ("X07", "room_d1", (60, 140, 230, 215)), ("X08", "room_d1", (1130, 366, 1290, 450)), ("X09", "room_d1", (840, 200, 1080, 520)),
     ("X10", "room_tornado", (1050, 96, 1370, 360)), ("X11", "room_d1", (960, 110, 1070, 330)), ("X12", "street_d1", (360, 466, 500, 524))]
for code, src, box in C:
    a = full[src][box[1]:box[3], box[0]:box[2]]
    save(a, f"/home/claude/envd/project/v3/px/{code}.png")
    print(code, a.shape[:2])
