class ConnectCountJob < ApplicationJob
  queue_as :default

  def perform
    # 先生宛てで生徒の接続数を通知
    User.where(teacher_flag: true).each do |teacher|
      LearningChannel.broadcast_to(teacher, {
        connected_count: User.where(connected_flag: true).count
        })
    end
  end
end
