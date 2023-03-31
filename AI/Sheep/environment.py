import torch
import numpy as np
import graphics as gr
import time

torch.manual_seed(0)

N_sheeps = 100
desired_distance = 1.0
k = desired_distance / (1.0 / np.sqrt(N_sheeps))
sheeps_coords = torch.rand((N_sheeps, 2)) * k

class SheepVisualizator:
    def __init__(self, size=(800, 800), scale=1, frequency=1, border=50):
        self.frequency = frequency
        self.size = size
        self.min_size = min(size)
        self.scale = scale
        self.last_update = 0
        self.border = border
        self.sheeps_points = []
        self.window = gr.GraphWin("Sheep environment", size[0], size[1], autoflush=False)
        gr.update()

    def update(self, sheeps_coords):
        if time.time() >= self.last_update + 1 / self.frequency:
            if len(self.sheeps_points) == 0:
                for i in range(len(sheeps_coords)):
                    sheep_point = gr.Circle(gr.Point(0, 0), 2)
                    sheep_point.id = "sheep" + str(i)
                    self.sheeps_points.append(sheep_point)

            for i, coord in enumerate(sheeps_coords):
                res = coord / self.scale * (self.min_size - self.border * 2) + self.border
                old_pos = torch.tensor([(self.sheeps_points[i].p1.x + self.sheeps_points[i].p2.x) / 2,
                                        (self.sheeps_points[i].p1.y + self.sheeps_points[i].p2.y) / 2])
                self.sheeps_points[i].move((res - old_pos)[0].item(), (res - old_pos)[1].item())
                if all(torch.logical_and(torch.greater(res, self.border), torch.less(res, self.min_size - self.border))):
                    if self.sheeps_points[i].canvas is None:
                        self.sheeps_points[i].draw(self.window)
                else:
                    if not self.sheeps_points[i].canvas is None:
                        self.sheeps_points[i].undraw()

            gr.update()
            self.last_update = time.time()

def find_in_dist(dist, coords, position):
    diff = coords - position
    mask = torch.less(torch.linalg.vector_norm(diff, dim=1), dist)
    return torch.masked_select(diff, mask.unsqueeze(-1)).view((-1, diff.size()[1]))

vis = SheepVisualizator(scale=3 * k)




vis.window.getMouse()

window.close()
