class ConnectCountJob < ApplicationJob
  queue_as :default

  def perform
    # 先生宛てで生徒の接続数を通知
    LearningChannel.broadcast_to(User.find(3), {
      connected_count: User.where(connected_flag: true).count
    })
  end
end
