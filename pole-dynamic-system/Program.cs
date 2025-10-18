// port of http://incompleteideas.net/book/code/pole.c
// copyright of the authors, see the link above for details.

const int N_BOXES = 162;
const int ALPHA = 1000;
const float BETA = 0.5f;
const float GAMMA = 0.95f;
const float LAMBDAw = 0.9f;
const float LAMBDAv = 0.8f;

const int MAX_FAILURES = 100;
const int MAX_STEPS = 100000;

float x, x_dot; // position and velocity of the cart
float theta, theta_dot; // angle and angular velocity of the pole

float[] w = new float[N_BOXES];
float[] v = new float[N_BOXES];
float[] e = new float[N_BOXES];
float[] xBar = new float[N_BOXES];


float p, oldP, rHat, r;
int box, steps = 0, failures = 0, failed;

for (var i = 0; i < N_BOXES; i++)
{
    w[i] = v[i] = xBar[i] = e[i] = 0.0f;
}

x = x_dot = theta = theta_dot = 0.0f;
box = getBox(x, x_dot, theta, theta_dot);

while (steps++ < MAX_STEPS && failures < MAX_FAILURES)
{
    var y =
}







static int getBox(float x, float x_dot, float theta, float theta_dot)
{
    const float one_degree = 0.0174532f;
    const float six_degrees = 0.1047192f;
    const float twelve_degrees = 0.2094384f;
    const float fifty_degrees = 0.87266f;

    int box = 0;

    if (x < -2.4f || x > 2.4f || theta < -twelve_degrees || theta > twelve_degrees)
    {
        return -1;
    }

    if (x >= -0.8f && x < 0.8f)
        box = 1;
    else
        box = 2;

    if (x_dot >= -0.5f && x_dot < 0.5f)
        box += 3;
    else
        box += 6;

    if (theta >= -six_degrees && theta < -one_degree)
        box += 9;
    else if (theta < 0.0f)
        box += 18;
    else if (theta < one_degree)
        box += 27;
    else if (theta < six_degrees)
        box += 36;
    else
        box += 45;

    if (theta_dot >= -fifty_degrees && theta_dot < fifty_degrees)
        box += 54;
    else
        box += 108;

    return box;
}
