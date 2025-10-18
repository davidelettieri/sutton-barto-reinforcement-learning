// port of http://incompleteideas.net/book/code/pole.c
// copyright of the authors, see the link above for details.

const int NumBoxes = 162;
const float Alpha = 1000.0f;
const float Beta = 0.5f;
const float Gamma = 0.95f;
const float LambdaW = 0.9f;
const float LambdaV = 0.8f;

const int MaxFailures = 100;
const int MaxSteps = 100000;

float x, xDot; // position and velocity of the cart
float theta, thetaDot; // angle and angular velocity of the pole

var w = new float[NumBoxes];
var v = new float[NumBoxes];
var e = new float[NumBoxes];
var xBar = new float[NumBoxes];


float p, oldP, r;
int steps = 0, failures = 0;

x = xDot = theta = thetaDot = 0.0f;
var box = GetBox(x, xDot, theta, thetaDot);

Console.WriteLine("Starting pole balancing. Select an integer number to seed the random instance:");
var a = int.Parse(Console.ReadLine() ?? "0");
var rand = new Random(a);

while (steps++ < MaxSteps && failures < MaxFailures)
{
    bool failed;
    var y = rand.NextSingle() < ProbPushRight(w[box]) ? 1 : 0;

    e[box] += (1 - LambdaW) * (y - 0.5f);
    xBar[box] += (1 - LambdaV);

    oldP = v[box];
    (x, xDot, theta, thetaDot) = CartPole(y, x, xDot, theta, thetaDot);

    box = GetBox(x, xDot, theta, thetaDot);

    if (box < 0)
    {
        failed = true;
        failures++;
        Console.WriteLine($"Failure {failures} at step {steps}");
        steps = 0;
        x = xDot = theta = thetaDot = 0.0f;
        box = GetBox(x, xDot, theta, thetaDot);

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

    var force = (action == 1) ? ForceMag : -ForceMag;
    var costheta = MathF.Cos(theta);
    var sintheta = MathF.Sin(theta);
    var temp = (force + PoleMassLength * theta_dot * theta_dot * sintheta) / TotalMass;
    var thetaacc = (Gravity * sintheta - costheta * temp) / (Length * (FourThirds - MassPole * costheta * costheta / TotalMass));
    var xacc = temp - PoleMassLength * thetaacc * costheta / TotalMass;
    x += Tau * x_dot;
    x_dot += Tau * xacc;
    theta += Tau * theta_dot;
    theta_dot += Tau * thetaacc;
    return (x, x_dot, theta, theta_dot);
}

static float ProbPushRight(float s)
{
    return 1.0f / (1.0f + MathF.Exp(-MathF.Max(-50.0f, MathF.Min(s, 50.0f))));
}

static int GetBox(float x, float x_dot, float theta, float theta_dot)
{
    const float OneDegree = 0.0174532f;
    const float SixDegrees = 0.1047192f;
    const float TwelveDegrees = 0.2094384f;
    const float FiftyDegrees = 0.87266f;

    if (x < -2.4f || x > 2.4f || theta < -TwelveDegrees || theta > TwelveDegrees)
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

    if (theta >= SixDegrees)
    {
        box += 45;
    }
    else if (theta >= OneDegree)
    {
        box += 36;
    }
    else if (theta >= 0.0f)
    {
        box += 27;
    }
    else if (theta >= -OneDegree)
    {
        box += 18;
    }
    else if (theta >= -SixDegrees)
    {
        box += 9;
    }

    if (theta_dot >= FiftyDegrees)
    {
        box += 108;
    }
    else if (theta_dot >= -FiftyDegrees)
    {
        box += 54;
    }

    return box;
}
