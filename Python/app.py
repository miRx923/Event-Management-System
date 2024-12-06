from flask import Flask, request, jsonify
import json

app = Flask(__name__)

# Recommendation Algorithm
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

    print("User Preferences:", user.preferences)
    print("Events Data:", [(e.name, e.tags) for e in events])

    if not user.preferences:
        recommendations = sorted(events, key=lambda e: len(e.tags), reverse=True)
    else:
        recommendations = [
            event for event in events
            if any(tag.strip() in user.preferences for tag in event.tags)
        ]

    print("Recommended Events:", [e.name for e in recommendations])
    return [event.name for event in recommendations]


# API Endpoint
@app.route('/recommend', methods=['POST'])
def recommend():
    data = request.get_json()
    user_data = data['user']
    events_data = data['events']

    recommendations = recommend_events(user_data, events_data)
    return jsonify({"recommendations": recommendations})

if __name__ == '__main__':
    app.run(debug=True, port=5001)