import json

class Event:
    def __init__(self, name, tags):
        self.name = name
        self.tags = tags

class User:
    def __init__(self, name, preferences):
        self.name = name
        self.preferences = preferences

def recommend_events(user_data, events_data):
    user = User(user_data['name'], user_data['preferences'])
    events = [Event(event['name'], event['tags']) for event in events_data]

    if not user.preferences:
        # Recommend the most popular events (by tag count)
        recommendations = sorted(events, key=lambda e: len(e.tags), reverse=True)
    else:
        # Recommend events matching user's preferences
        recommendations = [
            event for event in events
            if any(tag in user.preferences for tag in event.tags)
        ]

    # Return the recommendations as a list of event names
    return [event.name for event in recommendations]

if __name__ == "__main__":
    import sys
    input_data = json.loads(sys.stdin.read())  # Read JSON input from C#
    user_data = input_data['user']
    events_data = input_data['events']

    recommendations = recommend_events(user_data, events_data)

    # Output the recommendations as JSON
    print(json.dumps({"recommendations": recommendations}))
