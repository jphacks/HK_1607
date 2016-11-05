# Be sure to restart your server when you modify this file. Action Cable runs in a loop that does not support auto reloading.
class LearningChannel < ApplicationCable::Channel
  def subscribed
    stream_from "learning_channel"
  end

  def unsubscribed
    # Any cleanup needed when channel is unsubscribed
  end

  def speak(data)
    ActionCable.server.broadcast "learning_channel",
        { student_name: data["name"], message: data["message"], time: Time.now.strftime("%Y年%m月%d日 %H時%M分%S秒") }
  end
end
