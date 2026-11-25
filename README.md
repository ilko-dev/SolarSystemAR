# SolarSystemAR

An Android AR app (Unity 6) that places the solar system in your room. A cosmonaut guide shows the planets and the Sun and talks about them, and you can control it by voice in Ukrainian.

## How to use voice

Tap **Говорити** (bottom-right) and say a command. The guide turns cyan while listening.

- **On a phone:** speak. The first time, allow the microphone. Needs internet.
- **In the Unity Editor:** a text box opens instead. Type the phrase and press Enter. The answer appears in the Console.

You don't need exact phrases. Word endings don't matter, and extra words are fine: «покажи, будь ласка, Марс» works.

## What you can say

| To… | Say |
|---|---|
| Find out what the guide can do | «що ти вмієш», «допомога» |
| Show a planet or the Sun | «Марс», «покажи Юпітер», «Сонце» |
| Hear a fact | «цікавий факт», «розкажи про Сатурн» |
| Hear the discovery history | «історія», «хто відкрив» |
| See which questions you can ask | «які питання» |
| Ask a question | «чому Марс червоний?», «скільки супутників?», «чи можна там жити?», «яка температура Сонця?» |
| Start the tour | «почни екскурсію», then «далі» / «назад» |
| Pause or resume the planets | «пауза» / «продовж» |
| See the whole system | «огляд» |
| Take a closer look, or leave it | «детальніше» / «вийти» |
| Make the guide stop or repeat | «стоп» / «повтори» |

If you ask about a planet the guide isn't next to (for example «цікавий факт про Юпітер» while he is at Mercury), he flies there first.

«Сонячна система» means the whole system, not the Sun. If a phrase names both a planet and the Sun («Марс біля Сонця»), the planet is chosen.

## If the phone has no Ukrainian voice

Go to **Settings → Text-to-speech**, choose **Google**, and download **Ukrainian**.

## For developers

- The **Show Test UI** button appears only in the Unity Editor and in builds with **Development Build** ticked (File → Build Profiles). Untick it for a release build and the test UI is hidden.
- Voice phrases are in `Assets/Scripts/Voice/VoiceKeywords.cs`.
- Planet and Sun texts and questions are in `Assets/Data/Planets/` (edit them in the Inspector). The Sun is `SunData`; it is id 8 (`PlanetId.Sun`), kept last because Unity saves these ids as numbers.
- The Sun is selectable because it has a `PlanetOrbit` component (orbit speed 0, so it only spins) and is listed in **SimulationRoot → Solar System Controller → Planets**. Its data is in **AppManager → App Controller → Planet Catalog**.
- To run the voice tests, which don't need Unity: `dotnet test Tests/VoiceParser.Tests`
