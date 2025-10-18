// port of http://incompleteideas.net/book/code/pole.c
// copyright of the authors, see the link above for details.

const int NumBoxes = 162;
const int Alpha = 1000;
const float Beta = 0.5f;
const float Gamma = 0.95f;
const float LambdaW = 0.9f;
const float LambdaV = 0.8f;

const int MaxFailures = 100;
const int MaxSteps = 100000;

float x, x_dot; // position and velocity of the cart
float theta, theta_dot; // angle and angular velocity of the pole

float[] w = new float[NumBoxes];
float[] v = new float[NumBoxes];
float[] e = new float[NumBoxes];
float[] xBar = new float[NumBoxes];


float p, oldP, r;
int steps = 0, failures = 0;

x = x_dot = theta = theta_dot = 0.0f;
int box = getBox(x, x_dot, theta, theta_dot);

Console.WriteLine("Starting pole balancing. Select an integer number to seed the random instance:");
int a = int.Parse(Console.ReadLine() ?? "0");
var rand = new Random(a);

while (steps++ < MaxSteps && failures < MaxFailures)
{
    bool failed;
    var y = rand.NextSingle() < probPushRight(w[box]) ? 1 : 0;

    e[box] += (1 - LambdaW) * (y - 0.5f);
    xBar[box] += (1 - LambdaV);

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

    var rHat = r + Gamma * p - oldP;

    for (var i = 0; i < NumBoxes; i++)
    {
        w[i] += Alpha * rHat * e[i];
        v[i] += Beta * rHat * xBar[i];

        if (failed)
        {
            e[i] = 0.0f;
            xBar[i] = 0.0f;
        }
        else
        {
            e[i] *= LambdaW;
            xBar[i] *= LambdaV;
        }
    }
}

if (failures == MaxFailures)
{
    Console.WriteLine($"Pole not balanced. Stopping after {failures} failures.");
}
else
{
    Console.WriteLine($"Pole balanced successfully for at least {steps} steps");
}

static (float x, float x_dot, float theta, float theta_dot) CartPole(int action, float x, float x_dot, float theta, float theta_dot)
{
    const float Gravity = 9.8f;
    const float MassCart = 1.0f;
    const float MassPole = 0.1f;
    const float TotalMass = MassPole + MassCart;
    const float Length = 0.5f; // actually half the pole's length
    const float PoleMassLength = MassPole * Length;
    const float ForceMag = 10.0f;
    const float Tau = 0.02f; // seconds between state updates
    const float FourThirds = 4.0f / 3.0f;

    float force = (action == 1) ? ForceMag : -ForceMag;
    float costheta = MathF.Cos(theta);
    float sintheta = MathF.Sin(theta);
    float temp = (force + PoleMassLength * theta_dot * theta_dot * sintheta) / TotalMass;
    float thetaacc = (Gravity * sintheta - costheta * temp) / (Length * (FourThirds - MassPole * costheta * costheta / TotalMass));
    float xacc = temp - PoleMassLength * thetaacc * costheta / TotalMass;
    x += Tau * x_dot;
    x_dot += Tau * xacc;
    theta += Tau * theta_dot;
    theta_dot += Tau * thetaacc;
    return (x, x_dot, theta, theta_dot);
}

static float probPushRight(float s)
{
    return 1.0f / (1.0f + MathF.Exp(-MathF.Max(-50.0f, MathF.Min(s, 50.0f))));
}

static int getBox(float x, float x_dot, float theta, float theta_dot)
{
    const float one_degree = 0.0174532f;
    const float six_degrees = 0.1047192f;
    const float twelve_degrees = 0.2094384f;
    const float fifty_degrees = 0.87266f;

    if (x < -2.4f || x > 2.4f || theta < -twelve_degrees || theta > twelve_degrees)
    {
        return -1;
    }

    int box;
    if (x < -0.8f)
    {
        box = 0;
    }
    else if (x < 0.8f)
    {
        box = 1;
    }
    else
    {
        box = 2;
    }

    if (x_dot >= 0.5f)
    {
        box += 6;
    }
    else if (x_dot >= -0.5f)
    {
        box += 3;
    }

    if (theta >= six_degrees)
    {
        box += 45;
    }
    else if (theta >= one_degree)
    {
        box += 36;
    }
    else if (theta >= 0.0f)
    {
        box += 27;
    }
    else if (theta >= -one_degree)
    {
        box += 18;
    }
    else if (theta >= -six_degrees)
    {
        box += 9;
    }

    if (theta_dot >= fifty_degrees)
    {
        box += 108;
    }
    else if (theta_dot >= -fifty_degrees)
    {
        box += 54;
    }

    return box;
}
