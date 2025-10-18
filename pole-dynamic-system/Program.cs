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


float p, oldP, r;
int steps = 0, failures = 0;

for (var i = 0; i < N_BOXES; i++)
{
    w[i] = v[i] = xBar[i] = e[i] = 0.0f;
}

x = x_dot = theta = theta_dot = 0.0f;
int box = getBox(x, x_dot, theta, theta_dot);

while (steps++ < MAX_STEPS && failures < MAX_FAILURES)
{
    bool failed;
    var y = Random.Shared.NextSingle() < ProbPushRight(w[box]) ? 1 : 0;

    e[box] += (1 - LAMBDAw) * (y - 0.5f);
    xBar[box] += (1 - LAMBDAv);

    oldP = v[box];
    (x, x_dot, theta, theta_dot) = CartPole(y, x, x_dot, theta, theta_dot);

    box = getBox(x, x_dot, theta, theta_dot);

    if (box < 0)
    {
        failed = true;
        failures++;
        Console.WriteLine($"Failure {failures} at step {steps}");
        steps = 0;
        x = x_dot = theta = theta_dot = 0.0f;
        box = getBox(x, x_dot, theta, theta_dot);

        r = -1.0f;
        p = 0.0f;
    }
    else
    {
        failed = false;
        r = 0.0f;
        p = v[box];
    }

    var rHat = r + GAMMA * p - oldP;

    for (var i = 0; i < N_BOXES; i++)
    {
        w[i] += ALPHA * rHat * e[i];
        v[i] += BETA * rHat * xBar[i];

        if (failed)
        {
            e[i] = 0.0f;
            xBar[i] = 0.0f;
        }
        else
        {
            e[i] *= LAMBDAw;
            xBar[i] *= LAMBDAv;
        }
    }
}

if (failures == MAX_FAILURES)
{
    Console.WriteLine("Pole not balanced. Stopping after {0} failures.", failures);
}
else
{
    Console.WriteLine("Pole balanced successfully for at least {0} steps", steps);
}

static (float x, float x_dot, float theta, float theta_dot) CartPole(int action, float x, float x_dot, float theta, float theta_dot)
{
    const float gravity = 9.8f;
    const float masscart = 1.0f;
    const float masspole = 0.1f;
    const float total_mass = masspole + masscart;
    const float length = 0.5f; // actually half the pole's length
    const float polemass_length = masspole * length;
    const float force_mag = 10.0f;
    const float tau = 0.02f; // seconds between state updates
    const float fourthirds = 4.0f / 3.0f;

    float force = (action == 1) ? force_mag : -force_mag;
    float costheta = MathF.Cos(theta);
    float sintheta = MathF.Sin(theta);
    float temp = (force + polemass_length * theta_dot * theta_dot * sintheta) / total_mass;
    float thetaacc = (gravity * sintheta - costheta * temp) / (length * (fourthirds - masspole * costheta * costheta / total_mass));
    float xacc = temp - polemass_length * thetaacc * costheta / total_mass;
    x += tau * x_dot;
    x_dot += tau * xacc;
    theta += tau * theta_dot;
    theta_dot += tau * thetaacc;
    return (x, x_dot, theta, theta_dot);
}

static float ProbPushRight(float s)
{
    return 1.0f / (1.0f + MathF.Exp(Math.Max(-50.0f, Math.Min(s, 50.0f))));
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
