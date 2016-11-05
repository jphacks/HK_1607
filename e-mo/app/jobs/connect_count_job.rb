class ConnectCountJob < ApplicationJob
  queue_as :default

  # 先生宛てで生徒の接続数を通知
  def perform
    # 登録されている先生全てに対する処理
    User.where(teacher_flag: true).each do |teacher|
      # 接続数を通知
      LearningChannel.broadcast_to(teacher, {
        connected_count: User.where(connected_flag: true).count
      })
    end
  end
end
