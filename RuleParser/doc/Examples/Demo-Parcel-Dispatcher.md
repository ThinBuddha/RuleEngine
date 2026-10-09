# The Almost Sensible Parcel Company

A fictional dispatcher with 35 rules in 10 sequential containers. It chooses a vehicle, handling instructions, parcel cover, an estimated delivery time, and a price in imaginary credits. All prices and timings are invented for this demo.

Import `Demo-Parcel-Dispatcher.txt` into the workbench, choose **Fire once** or **Iteration**, and execute. Clear the fact context before trying a different scenario. The file begins with `enforce model "Demo-Parcel-Dispatcher-model.txt"`, referring to the companion [model definitions](Demo-Parcel-Dispatcher-model.txt). The parser records this filename; model loading and enforcement are not implemented yet. The file uses ordered stages and `Vehicle.Selection.status` ("pending" or "selected") to make the first matching vehicle rule win. Iteration retries unmatched rules but skips each rule after it fires, so classification and vehicle-selection initialization run only once per evaluation. Both modes produce the same results for this demo. Deduction does not resolve dependencies across these containers.

The model file contains units and types followed by classes that show how the 33 typed fact IDs form a nested model. Import it in the GUI's Model workspace to view the two sections in their respective tabs.

## What to show

An unasserted Boolean fact remains unknown; absence does not mean false. This demo therefore represents both outcomes explicitly with string statuses: `Parcel.Drone.status` and `Destination.Drone.status` are "allowed" or "disallowed", and `Flight.Status` is "available" or "unavailable". It does not encode Boolean facts as numbers or use negated conclusions. Numeric fees, priorities, distances, and times remain numeric.

The input prompts offer choices for parcel contents, size, weather, recipient location, urgency, and membership. Distance in kilometres and declared parcel value use numeric input; enter nonnegative numbers.

Each stage establishes facts consumed by later stages:

1. Contents determine handling, packing fees, and whether the parcel may fly.
2. Size determines an additional packing fee.
3. Distance determines the delivery zone and route fee.
4. Weather determines flight availability and delays.
5. Recipient location determines handoff instructions and whether drone delivery is possible.
6. Urgency determines service fees and waiting time.
7. The first matching vehicle rule wins: chilled cargo, regional distance, storms, and large parcels take precedence over the drone; locker delivery precedes the bike; a van is the fallback.
8. Membership determines a discount of 0, 1, or 2 credits.
9. Declared value determines parcel cover and its fee.
10. The quote combines all fees, applies the discount, and calculates the estimated delivery time.

## Demo scenarios

| Scenario | Contents / size | Distance | Weather | Recipient | Urgency | Membership | Declared value | Expected vehicle | Total credits | Minutes |
|---|---|---:|---|---|---|---|---:|---|---:|---:|
| A book that absolutely must fly | books / small | 2 | clear | home | express | visitor | 25 | drone | 23 | 8 |
| Flowers in the rain | flowers / small | 2 | rain | home | today | regular | 75 | bike courier | 11 | 85 |
| Ice cream defeats the locker plan | ice cream / medium | 2 | clear | parcel locker | express | neighbourhood hero | 20 | refrigerated van | 26 | 45 |
| The drone loses to a storm | electronics / small | 2 | storm | home | express | visitor | 300 | delivery van | 25 | 130 |
| A very large box of books | books / large | 30 | clear | office | relaxed | neighbourhood hero | 25 | regional van | 21 | 210 |
| An ordinary locker delivery | electronics / medium | 5 | clear | parcel locker | today | regular | 100 | locker shuttle | 16 | 105 |

Inspect the **Evaluation trace** for the selected rules and **Fact context** for `Delivery.Vehicle`, `Parcel.Handling`, `Delivery.Handoff`, `Cover.Level`, `Quote.Total`, and `Delivery.Estimated.minutes`.

Chilled goods override both the vehicle and the handoff instruction: even when the customer requests a parcel locker, the model requires a refrigerated van and an in-person handoff. This makes a useful example of a more specific rule taking precedence over a customer's preference.

Try changing one input after clearing the context: make the weather a storm, change books to ice cream, or move the destination outside the neighbourhood. The trace shows why the earlier vehicle choice no longer applies.
