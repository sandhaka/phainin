# Multithreaded Market Data Processor Demo

A .NET multithreading demo designed to model a **performance-critical market-data processing system**.

The project is intentionally more than a simple producer/consumer example. It models a small financial-market environment where multiple concurrent actors generate market activity, a venue matches orders and publishes market-data events, and a downstream **Market Data Processor** reconstructs the market state and calculates derived information.

The main purpose of the project is to create a realistic environment in which we can study:

- multithreading;
- synchronization primitives;
- race conditions;
- contention;
- variable numbers of worker threads;
- CPU-intensive processing;
- asynchronous persistence;
- event ordering;
- consistency between concurrent components;
- performance and scalability.

The production target is currently **.NET**.

---

# Chapter 1 — Financial Concepts

## 1.1 What are we modelling?

The demo models a simplified **electronic financial market**.

The central idea is:

```text
Participants
     |
     | buy / sell orders
     v
   Venue
     |
     | market-data events
     v
Market Data Processor
     |
     +--> reconstructed order books
     +--> market statistics
     +--> persistence
```

A real financial market contains many participants sending orders to an exchange or other trading venue.

The venue receives those orders, maintains an order book, matches compatible orders and publishes information about what is happening in the market.

The downstream Market Data Processor does **not** own the authoritative market state. It receives a stream of events and reconstructs a local representation of the market.

This distinction is important for the demo.

---

## 1.2 Participants

Participants represent entities that interact with the market.

They can generate actions such as:

- adding liquidity;
- cancelling orders;
- sending aggressive orders that may execute against existing liquidity.

For the purposes of the demo, participants do not need to represent realistic trading strategies.

They are primarily **sources of concurrent activity**.

For example:

```text
Participant A
    -> BUY 100 @ 100.00

Participant B
    -> SELL 50 @ 101.00

Participant C
    -> SELL 30 @ 100.00
```

The resulting market state depends on the interaction between these orders.

---

## 1.3 Buy and Sell

Every order has a side:

```text
BUY
SELL
```

A BUY order expresses willingness to acquire an asset.

A SELL order expresses willingness to sell an asset.

The market therefore naturally has two sides:

```text
BUY SIDE                    SELL SIDE

100.00 -> 500               100.01 -> 200
 99.99 -> 300               100.02 -> 450
 99.98 -> 700               100.03 -> 100
```

The numbers represent available quantity at each price.

---

## 1.4 The Order Book

An **order book** represents the currently available liquidity for a particular instrument.

For example:

```text
              ORDER BOOK

BUY                         SELL
-------------------         -------------------
Price       Qty             Price       Qty

100.00      500             100.01      200
 99.99      300             100.02      450
 99.98      700             100.03      100
```

The best BUY price is called the:

**Best Bid**

The best SELL price is called the:

**Best Ask**

Therefore:

```text
Best Bid = 100.00
Best Ask = 100.01
```

---

## 1.5 Spread

The difference between the best ask and best bid is the **spread**.

```text
Spread = Best Ask - Best Bid
```

In the example:

```text
100.01 - 100.00 = 0.01
```

The spread is one of the simplest pieces of derived market information.

It is useful for the demo because it requires the processor to maintain a consistent view of both sides of the order book.

---

## 1.6 Mid Price

The midpoint between the best bid and best ask is:

```text
Mid Price = (Best Bid + Best Ask) / 2
```

For the example:

```text
(100.00 + 100.01) / 2 = 100.005
```

This is derived information; it does not come directly from an individual order.

---

## 1.7 Liquidity

Liquidity describes the quantity available for trading.

For example:

```text
BUY

100.00 -> 500
 99.99 -> 300
 99.98 -> 700
```

There are:

```text
500 + 300 + 700 = 1500
```

units of visible BUY liquidity across those levels.

The demo can use liquidity changes to generate substantial event traffic.

---

## 1.8 Limit Orders

A limit order specifies a price.

Example:

```text
BUY 100 @ 100.00
```

This means:

> Buy up to 100 units at a price no higher than 100.00.

Similarly:

```text
SELL 200 @ 101.00
```

means:

> Sell up to 200 units at a price no lower than 101.00.

Limit orders can therefore add liquidity to the order book.

---

## 1.9 Aggressive Orders

An order can interact with existing liquidity instead of simply waiting in the book.

For example:

```text
Current book:

BUY              SELL
100.00 -> 500    100.01 -> 200
```

A sufficiently aggressive BUY order can consume the SELL liquidity at 100.01.

This produces a **trade**.

---

## 1.10 Trades

A trade is an actual execution.

For example:

```text
BUY 100 @ 100.01
```

matches:

```text
SELL 100 @ 100.01
```

The result is:

```text
TRADE
Quantity = 100
Price    = 100.01
```

A crucial distinction for the demo is:

> A trade is not itself an order-book update.

The execution causes the affected liquidity in the order book to change.

The market-data stream can therefore contain both:

```text
BOOK UPDATE
TRADE
```

as separate event types.

The processor uses both:

- book updates to reconstruct market depth;
- trades to calculate trade-related statistics.

---

## 1.11 Where Do Market Changes Come From?

The market does not need an artificial component that randomly changes prices independently of everything else.

The underlying changes originate from participant activity.

Conceptually:

```text
Participants
     |
     +--> add BUY liquidity
     |
     +--> add SELL liquidity
     |
     +--> cancel existing orders
     |
     +--> send aggressive orders
                 |
                 v
              MATCH
                 |
                 v
               TRADE
```

Therefore the market evolves as a consequence of order activity.

A simulator may still be used, but its purpose is to **generate participant activity**, not to arbitrarily manipulate the final market price.

---

## 1.12 The Venue

The **Venue** represents the exchange/trading venue.

It is the authoritative source of market state in this demo.

Conceptually it contains:

```text
Venue
 |
 +-- Participants / incoming order flow
 |
 +-- Matching Engine
 |
 +-- Authoritative Order Book
 |
 +-- Market Data Publisher
```

The venue receives participant actions, processes them through the matching engine, modifies its authoritative order book and publishes the resulting market-data events.

---

## 1.13 Market-by-Price

The demo uses a **Market-by-Price (MBP)** representation.

Instead of exposing every individual order, the market-data stream reports the **aggregate quantity available at a price level**.

For example:

```text
BUY 100.00 -> 500
```

means that the total visible BUY quantity at 100.00 is 500.

If the quantity changes:

```text
BUY 100.00 -> 350
```

the processor replaces the previous value.

If the quantity becomes zero:

```text
BUY 100.00 -> 0
```

the price level is removed.

This is intentionally simpler than Market-by-Order and is sufficient for the concurrency demonstration.

---

## 1.14 Market Data Events

The Venue publishes events describing changes in the market.

Conceptually:

```text
MarketDataEvent
    |
    +-- DepthUpdate
    |
    +-- Trade
```

A `DepthUpdate` describes the resulting aggregate quantity for:

```text
Instrument
Venue
Side
Price
Quantity
```

A quantity of zero means:

```text
remove this price level
```

A `Trade` contains information such as:

```text
Instrument
Venue
Price
Quantity
Timestamp
```

The exact implementation can evolve, but the important architectural distinction is that **depth updates and trades are separate pieces of information**.

---

## 1.15 Market Data Feed

The Market Data Feed sits between the Venue and the Market Data Processor.

Its responsibilities can include:

- receiving events from the venue;
- validating events;
- normalizing them;
- assigning or preserving sequence information;
- delivering them to consumers.

Conceptually:

```text
Venue
  |
  v
Market Data Feed
  |
  v
Market Data Processor
```

The feed is therefore an abstraction around the event stream rather than the authoritative market itself.

---

## 1.16 Per-Venue Order Books

The processor should maintain a separate reconstructed book for each:

```text
Venue + Instrument
```

For example:

```text
Venue A / EURUSD
Venue B / EURUSD
Venue C / EURUSD
```

This is important because the same instrument may trade simultaneously on multiple venues.

The processor must not accidentally merge their state before it is appropriate to do so.

---

## 1.17 Market Statistics

Once the order books and trades are reconstructed, the processor can calculate derived information.

Examples include:

```text
Best Bid
Best Ask
Spread
Mid Price
Total Visible Volume
Trade Volume
VWAP
OHLC
```

### VWAP

Volume Weighted Average Price:

```text
VWAP = Σ(price × quantity) / Σ(quantity)
```

For example:

```text
Trade 1: 100 @ 10
Trade 2: 200 @ 11

VWAP = (100×10 + 200×11) / 300
     = 10.666...
```

### OHLC

OHLC represents:

```text
Open
High
Low
Close
```

for a defined time interval.

For example, a one-second, one-minute or five-minute interval.

These calculations are useful because they introduce additional CPU work and state management into the processor.

---

# Chapter 2 — High-Level Architecture

## 2.1 Overall Architecture

The complete conceptual architecture is:

```text
                           MARKET SIMULATION
                                  |
                                  v
                       +----------------------+
                       |      Participants    |
                       |                      |
                       | BUY / SELL / CANCEL  |
                       +----------+-----------+
                                  |
                                  v
                       +----------------------+
                       |        Venue         |
                       |                      |
                       |  +----------------+  |
                       |  | Matching Engine |  |
                       |  +-------+--------+  |
                       |          |           |
                       |  +-------v--------+  |
                       |  | Authoritative  |  |
                       |  | Order Book     |  |
                       |  +-------+--------+  |
                       |          |           |
                       |  +-------v--------+  |
                       |  | Market Data    |  |
                       |  | Publisher      |  |
                       |  +----------------+  |
                       +----------+-----------+
                                  |
                                  v
                       +----------------------+
                       |   Market Data Feed   |
                       |                      |
                       | validation           |
                       | normalization        |
                       | sequencing           |
                       +----------+-----------+
                                  |
                                  v
                +-------------------------------------+
                |      Market Data Processor          |
                |                                     |
                |  +-------------------------------+  |
                |  | Per-Venue Order Books         |  |
                |  +-------------------------------+  |
                |                                     |
                |  +-------------------------------+  |
                |  | Trade Processing              |  |
                |  +-------------------------------+  |
                |                                     |
                |  +-------------------------------+  |
                |  | Market Statistics             |  |
                |  +-------------------------------+  |
                +----------------+--------------------+
                                 |
                                 v
                      +----------------------+
                      |   Async Persistence  |
                      |                      |
                      | files / database     |
                      +----------------------+
```

---

## 2.2 The Two Different Worlds

The architecture deliberately separates two worlds.

### Market world

```text
Participants
    |
    v
Venue
    |
    v
Market events
```

The Venue owns the authoritative state.

### Processing world

```text
Market events
    |
    v
Market Data Processor
    |
    +--> reconstructed state
    +--> analytics
    +--> persistence
```

The processor owns only a **copy/reconstruction** of the market state.

This distinction is fundamental when testing consistency.

---

## 2.3 Venue as the Source of Truth

The Venue contains the authoritative order book.

For example:

```text
Venue.OrderBook

BUY
100.00 -> 500
 99.99 -> 300

SELL
100.01 -> 200
100.02 -> 400
```

The processor receives events and reconstructs:

```text
Processor.OrderBook

BUY
100.00 -> 500
 99.99 -> 300

SELL
100.01 -> 200
100.02 -> 400
```

The processor should eventually converge to the same state, assuming:

- all events are received;
- events are processed correctly;
- ordering guarantees are respected;
- there are no implementation errors.

---

## 2.4 Event Flow

A typical sequence is:

```text
1. Participant creates order
        |
2. Venue receives order
        |
3. Matching Engine processes order
        |
4. Authoritative Order Book changes
        |
5. Venue publishes DepthUpdate
        |
6. Feed delivers event
        |
7. Processor applies DepthUpdate
        |
8. Processor updates derived statistics
        |
9. Processor asynchronously persists results
```

For an execution:

```text
1. Aggressive order arrives
        |
2. Matching Engine finds liquidity
        |
3. Trade occurs
        |
4. Authoritative book changes
        |
5. Venue publishes DepthUpdate
        |
6. Venue publishes Trade
        |
7. Processor applies both
        |
8. Trade statistics are updated
```

The exact ordering guarantees must be explicitly defined by the implementation.

---

## 2.5 Concurrency Model

The system is intentionally concurrent.

Possible parallel activities include:

```text
Participant Thread 1
Participant Thread 2
Participant Thread 3
Participant Thread N

        |

        v

Venue processing

        |

        v

Market-data publishing

        |

        +-------------------+
        |                   |
        v                   v
Processor workers      Persistence workers
```

The number of participants and processing workers should be configurable.

For example:

```text
1 thread
2 threads
4 threads
8 threads
16 threads
...
```

This allows scalability and contention experiments.

---

# Chapter 3 — Detailed Modelling

This chapter describes the components conceptually.

---

## 3.1 Participant

A Participant generates market activity.

### Responsibilities

A participant can:

```text
Create BUY orders
Create SELL orders
Cancel orders
Create aggressive orders
```

The participant does not directly modify the order book.

Instead:

```text
Participant
      |
      v
Order / Action
      |
      v
Venue
```

This preserves the important ownership boundary.

### Possible implementation

```text
Participant
    GenerateAction()
        -> Buy
        -> Sell
        -> Cancel
        -> AggressiveOrder
```

The participant generator can be deterministic when running tests.

That is important because a failing concurrent test should be reproducible.

---

## 3.2 Market Simulator

The simulator controls the generation of market activity.

It should not simply do:

```text
price += randomNumber;
```

Instead it should generate actions that cause the market to evolve naturally.

Conceptually:

```text
Market Simulator
       |
       +--> Participant A
       +--> Participant B
       +--> Participant C
       +--> ...
```

The simulator can control:

- number of participants;
- activity rate;
- order sizes;
- price distribution;
- cancellation rate;
- aggressive-order rate;
- number of instruments;
- duration of the simulation.

The simulator therefore creates the **input workload** for the concurrency system.

---

## 3.3 Order / Action

An action represents something a participant wants to do.

Possible types:

```text
AddLimitOrder
CancelOrder
AggressiveOrder
```

A limit order contains at least:

```text
Instrument
Side
Price
Quantity
OrderId
ParticipantId
```

The important property is that the action itself does not change shared state.

The Venue decides what actually happens.

---

## 3.4 Venue

The Venue is one of the most important components.

It represents the exchange/trading venue and owns the authoritative market state.

Conceptually:

```text
Venue
 |
 +-- Order Input
 |
 +-- Matching Engine
 |
 +-- Authoritative Order Book
 |
 +-- Market Data Publisher
```

### Responsibilities

The Venue:

1. receives participant actions;
2. processes them;
3. modifies the authoritative order book;
4. produces executions;
5. publishes market-data events.

The Venue is therefore the boundary between **market activity** and **market data**.

---

## 3.5 Matching Engine

The Matching Engine determines whether an incoming order can interact with existing liquidity.

Example:

```text
Existing:

SELL
100.01 -> 200
100.02 -> 300
```

Incoming:

```text
BUY 250
```

The matching engine can execute:

```text
200 @ 100.01
 50 @ 100.02
```

Result:

```text
TRADE 200 @ 100.01
TRADE  50 @ 100.02
```

and the remaining book becomes:

```text
SELL
100.02 -> 250
```

The matching engine therefore modifies the authoritative order book.

---

## 3.6 Authoritative Order Book

The Venue's order book is the source of truth.

A useful conceptual representation is:

```text
OrderBook
 |
 +-- Instrument
      |
      +-- BUY price levels
      |
      +-- SELL price levels
```

Each side can be represented as:

```text
Price -> Aggregate Quantity
```

For example:

```text
BUY

100.00 -> 500
 99.99 -> 300
 99.98 -> 100
```

and:

```text
SELL

100.01 -> 200
100.02 -> 450
100.03 -> 100
```

---

## 3.7 Market Data Publisher

The publisher converts changes in the authoritative market state into external events.

For example:

```text
Before:

BUY 100.00 -> 500

Order consumes 200:

After:

BUY 100.00 -> 300
```

The publisher emits:

```text
DepthUpdate
Side     = BUY
Price    = 100.00
Quantity = 300
```

The processor does not need to know the individual orders that produced this state.

---

## 3.8 Market Data Feed

The feed provides a boundary between the Venue and consumers.

Conceptually:

```text
Venue Publisher
       |
       v
     Feed
       |
       v
  Processor
```

Possible responsibilities:

```text
Validation
Normalization
Sequencing
Transport
Backpressure handling
```

The feed should preserve the semantics of the event stream.

If sequence numbers are used, they allow the processor to detect:

```text
missing events
duplicate events
out-of-order events
```

---

## 3.9 Market Data Processor

The Market Data Processor is the main component being stressed.

Its job is to consume the event stream and reconstruct useful market information.

Conceptually:

```text
             Market Data Processor

                    |
        +-----------+-----------+
        |           |           |
        v           v           v
    Book State   Trades     Statistics
        |
        v
   Persistence
```

---

## 3.10 Reconstructed Order Book

The processor maintains its own copy of the book.

For each:

```text
Venue + Instrument
```

it applies incoming `DepthUpdate` events.

Example:

```text
Current:

BUY
100.00 -> 500
 99.99 -> 300
```

Event:

```text
BUY
100.00 -> 350
```

Processor state becomes:

```text
BUY
100.00 -> 350
 99.99 -> 300
```

Event:

```text
BUY
100.00 -> 0
```

Processor removes:

```text
100.00
```

---

## 3.11 Trade Processor

Trade events are processed independently from depth updates.

Example:

```text
Trade
Price    = 100.01
Quantity = 200
```

The processor can update:

```text
Total traded volume
VWAP
OHLC
Trade count
Last traded price
```

The trade itself does not directly modify the reconstructed book.

The corresponding `DepthUpdate` does that.

This separation is important because it mirrors the semantics of the market-data stream.

---

## 3.12 Market Statistics

The processor calculates derived information from its reconstructed state.

Examples:

```text
Best Bid
Best Ask
Spread
Mid Price
Visible Volume
Trade Volume
VWAP
OHLC
```

For example:

```text
Book:

BUY  100.00
SELL 100.02

Spread = 0.02
Mid    = 100.01
```

These calculations are intentionally useful as CPU workloads.

---

## 3.13 Persistence

Market data and/or calculated statistics must eventually be saved to disk.

Persistence is asynchronous.

The processor should therefore avoid making the main market-data processing path wait for disk I/O.

Conceptually:

```text
Processor
    |
    +--> calculate
    |
    +--> enqueue persistence record
                 |
                 v
        Persistence Worker
                 |
                 v
                Disk
```

This introduces another concurrency boundary.

The persistence layer therefore becomes part of the testing problem rather than simply an implementation detail.

---

## 3.14 Asynchronous Persistence Queue

A persistence queue decouples CPU-intensive processing from slower I/O.

Conceptually:

```text
Producer threads
      |
      v
+-------------+
| Persistence |
|    Queue    |
+-------------+
      |
      v
Persistence Worker(s)
      |
      v
    Disk
```

The queue must correctly handle:

- multiple producers;
- one or multiple consumers;
- shutdown;
- backpressure;
- queue growth;
- ordering requirements;
- data loss prevention.

---

## 3.15 Threading Boundaries

A major objective of the project is to make threading boundaries explicit.

Potential boundaries include:

```text
Participant -> Venue
Venue -> Feed
Feed -> Processor
Processor -> Persistence
```

Each boundary should have clearly defined ownership.

For example:

```text
Who owns the event?
Who is allowed to mutate it?
Can it be reused?
Is it immutable?
Who owns the queue?
Who is responsible for shutdown?
```

These questions are more important than simply choosing `lock` versus `Monitor` versus another synchronization primitive.

---

## 3.16 Race Conditions

Race conditions are a primary concern of the project.

A typical example:

```text
Thread A:
    read quantity = 100

Thread B:
    read quantity = 100

Thread A:
    quantity = 70

Thread B:
    quantity = 50
```

The expected result might have been:

```text
20
```

but the actual result becomes:

```text
50
```

because the updates were not synchronized correctly.

The test framework should therefore be able to detect discrepancies between:

```text
Expected state
```

and:

```text
Observed state
```

---

## 3.17 Deterministic Reproduction

A concurrency failure that happens once in 10 million operations is difficult to investigate.

The simulator should therefore support deterministic inputs.

For example:

```text
Seed = 12345
Participants = 8
Operations = 1,000,000
```

A failed execution should ideally be reproducible with the same:

```text
seed
configuration
event sequence
```

This allows the test framework to distinguish between:

```text
random stress
```

and:

```text
reproducible concurrency bug
```

---

## 3.18 Consistency Invariants

The most valuable tests should verify invariants rather than simply checking whether the application crashes.

Examples:

### Order-book invariant

```text
Quantity >= 0
```

### Depth consistency

After processing all events:

```text
Processor Book == Venue Book
```

when comparing at the same sequence point.

### Trade consistency

```text
Processed Trade Volume
==
Venue Generated Trade Volume
```

### Persistence consistency

```text
Persisted Events
==
Events that were committed for persistence
```

### Sequence consistency

```text
Expected Sequence + 1
==
Received Sequence
```

unless gaps are explicitly supported.

---

## 3.19 The Fundamental Test Model

The complete test model can therefore be summarized as:

```text
                 INPUT WORKLOAD
                       |
                       v
              +----------------+
              |   Participants |
              +-------+--------+
                      |
                      v
              +---------------+
              |     Venue     |
              |               |
              | Matching      |
              | Order Book    |
              +-------+-------+
                      |
                      | authoritative events
                      v
              +---------------+
              | Market Feed   |
              +-------+-------+
                      |
                      v
              +-----------------------+
              | Market Data Processor |
              |                       |
              | Book reconstruction   |
              | Trade processing      |
              | Statistics            |
              +----------+------------+
                         |
                         v
                  Async Persistence
                         |
                         v
                        Disk
```

The core correctness property is:

```text
Participant actions
        ↓
Authoritative Venue state
        ↓
Published event stream
        ↓
Processor reconstructed state
        ↓
Persisted state
```

Every stage should preserve the required semantics.

The project can then be used to deliberately introduce different synchronization strategies and measure their effects on:

- correctness;
- throughput;
- latency;
- contention;
- CPU utilization;
- memory usage;
- scalability;
- persistence pressure.

The financial domain is therefore not the final goal of the project. It provides a realistic workload and state model in which **multithreading correctness and performance problems become meaningful and measurable**.
